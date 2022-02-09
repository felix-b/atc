using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World;
using Atc.World.LLHZ;
using AtcProto;
using ProtoBuf.WellKnownTypes;
using Zero.Latency.Servers;
using Zero.Loss.Actors;
using AircraftActor = Atc.World.Traffic.AircraftActor;

namespace Atc.Server
{
    public partial class WorldService
    {
        private readonly ISupervisorActor _supervisor;
        private readonly WorldActor _world;
        private readonly ILogger _logger;
        //private readonly TempMockLlhzRadio _tempMockRadio;//TODO: temporary; remove
        private readonly UserRadioMonitor _userRadio;

        public WorldService(
            ISupervisorActor supervisor, 
            WorldActor world, 
            ILogger logger, 
            //TempMockLlhzRadio tempMockRadio, 
            UserRadioMonitor userRadio)
        {
            _supervisor = supervisor;
            _world = world;
            _logger = logger;
            _userRadio = userRadio;
            //_tempMockRadio = tempMockRadio;
        }

        [PayloadCase(ClientToServer.PayloadOneofCase.connect)]
        public void Connect(IDeferredConnectionContext<ServerToClient> connection, ClientToServer envelope)
        {
            _logger.ConnectRequestReceived(connection.Id, envelope.connect.Token);
            
            if (envelope.connect.Token == "T12345")
            {
                _logger.ConnectionAuthenticated(connection.Id);

                connection.Session.Add<ClientClaims.Authenticated>();
                connection.FireMessage(new ServerToClient() {
                    reply_connect = new () {
                        ServerBanner = $"Hello new client, your connection id is {connection.Id}"
                    }
                });
            }
            else
            {
                _logger.ConnectionAuthenticationFailed(connection.Id, reason: "TokenNotValid");
                connection.RequestClose();
            }
        }

        [PayloadCase(ClientToServer.PayloadOneofCase.query_traffic)]
        public void QueryTraffic(IDeferredConnectionContext<ServerToClient> connection, ClientToServer message)
        {
            ValidateAuthentication(connection); //TODO: move this to a middleware?

            var requestId = message.Id;
            var request = message.query_traffic;
            var rect = new GeoRect(new(request.MinLat, request.MinLon), new(request.MaxLat, request.MaxLon));

            var query = _world.QueryTraffic(in rect);
            var subscription = query.Subscribe(ObserveTrafficQuery);
            connection.RegisterObserver(subscription, request.CancellationKey);

            var replyMessage = CreateReplyMessage(message, query);
            var foundAircraftCount = replyMessage.reply_query_traffic.TrafficBatchs.Count;
            
            connection.FireMessage(replyMessage);
            _logger.SentTrafficQueryResults(connection.Id, foundAircraftCount);
            _logger.TrafficQueryObserverCreated(
                connection.Id, 
                request.CancellationKey, 
                request.MinLat, 
                request.MinLon,
                request.MaxLat,
                request.MinLon);

            void ObserveTrafficQuery(in QueryObservation<AircraftActor> observation)
            {
                if (!connection.IsActive)
                {
                    _logger.TrafficQueryObserverConnectionNotActive(connection.Id);
                    return;
                }
                
                using (_logger.TrafficQueryObserverPushingNotifications(connection.Id))
                {
                    int updated = PushAircraftUpdatedNotifications(observation.Updated, connection, requestId);
                    int added = PushAircraftAddedNotifications(observation.Added, connection, requestId);
                    int removed = PushAircraftRemovedNotifications(observation.Removed, connection, requestId);
                    
                    _logger.TrafficQueryObserverCompleted(connection.Id, updated, added, removed);
                    connection.RequestFlush();
                }
            }

            static ServerToClient CreateReplyMessage(ClientToServer incoming, IObservableQuery<AircraftActor> query)
            {
                var request = incoming.query_traffic;
                var results = query.GetResults();
                var reply = new ServerToClient() {
                    ReplyToRequestId = incoming.Id,
                    reply_query_traffic = new() {
                        MinLat = request.MinLat, MinLon = request.MinLon, MaxLat = request.MaxLat, MaxLon = request.MaxLon,
                    }
                };
                reply.reply_query_traffic.TrafficBatchs.AddRange(results.Select(CreateAircraftMessage));
                return reply;
            }
        }

        [PayloadCase(ClientToServer.PayloadOneofCase.cancel_traffic_query)]
        public void CancelTrafficQuery(IDeferredConnectionContext<ServerToClient> connection, ClientToServer message)
        {
            connection.DisposeObserver(message.cancel_traffic_query.CancellationKey);
        }

        [PayloadCase(ClientToServer.PayloadOneofCase.user_acquire_aircraft)]
        public void UserAcquireAircraft(IDeferredConnectionContext<ServerToClient> connection, ClientToServer message)
        {
            var request = message.user_acquire_aircraft;
            //_tempMockRadio.ResetFlight();
            
            var airport = _supervisor.GetAllActorsOfType<LlhzAirportActor>().Single().Get();
            var aircraft = airport.GetAircraftByCallsign("4XCGK").Get();
            var info = airport.Information;
            
            //TODO - implement the logic
            connection.FireMessage(new ServerToClient {
                reply_user_acquire_aircraft = new ServerToClient.ReplyUserAcquireAircraft {
                    Success = true,
                    AircraftId = aircraft.Id,
                    Callsign = aircraft.TailNo,
                    Weather = new WeatherMessage {
                        QnhHpa = (int)info.Qnh.Hpa,
                        WindSpeedKt = (int)info.Wind.Speed.GetValueOrDefault(Speed.FromKnots(0)).Max.Knots,
                        WindTrueBearingDegrees = (int)info.Wind.Direction.GetValueOrDefault(Bearing.FromTrueDegrees(0)).Max.Degrees
                    }  
                }
            });
        }

        [PayloadCase(ClientToServer.PayloadOneofCase.user_update_aircraft_situation)]
        public void UserUpdateAircraftSituation(IDeferredConnectionContext<ServerToClient> connection, ClientToServer message)
        {
            var update = message.user_update_aircraft_situation;
            var com1Khz = update.Situation.MonitoringFrequencyKhzs.FirstOrDefault();
            
            _userRadio.TuneTo(Frequency.FromKhz(com1Khz));
            _logger.TunedUserRadioMonitor(frequencyKhz: com1Khz);
            
            //TODO implement more updates
        }

        [PayloadCase(ClientToServer.PayloadOneofCase.user_release_aircraft)]
        public void UserReleaseAircraft(IDeferredConnectionContext<ServerToClient> connection, ClientToServer message)
        {
            //TODO - implement the logic
        }

        //TODO: temporary; remove
        [PayloadCase(ClientToServer.PayloadOneofCase.user_ptt_pressed)]
        public void UserPttPressed(IDeferredConnectionContext<ServerToClient> connection, ClientToServer message)
        {
            //Console.WriteLine("WorldService::UserPttPressed");
            //var request = message.user_ptt_pressed;
            //_tempMockRadio.PttPushed((int)request.FrequencyKhz);
        }

        //TODO: temporary; remove
        [PayloadCase(ClientToServer.PayloadOneofCase.user_ptt_released)]
        public void UserPttReleased(IDeferredConnectionContext<ServerToClient> connection, ClientToServer message)
        {
            //Console.WriteLine("WorldService::UserPttReleased");
            //var request = message.user_ptt_released;
            //_tempMockRadio.PttReleased((int)request.FrequencyKhz);
        }

        private void ValidateAuthentication(IDeferredConnectionContext<ServerToClient> connection)
        {
            if (!connection.Session.Has<ClientClaims.Authenticated>())
            {
                throw _logger.ConnectionWasNotAuthenticated(connection.Id);
            }
        }

        private int PushAircraftAddedNotifications(
            IEnumerable<AircraftActor> allAddedAircraft,
            IDeferredConnectionContext<ServerToClient> connection,
            ulong replyToMessageId)
        {
            var count = 0;
            
            foreach (var added in allAddedAircraft)
            {
                count++;
                connection.FireMessage(new ServerToClient() {
                    ReplyToRequestId = replyToMessageId,
                    notify_aircraft_created = new() {
                        Aircraft = CreateAircraftMessage(added)
                    }
                });
            }
            
            return count;
        }

        private int PushAircraftUpdatedNotifications(
            IEnumerable<AircraftActor> allUpdatedAircraft,
            IDeferredConnectionContext<ServerToClient> connection,
            ulong replyToMessageId)
        {
            var count = 0;
            
            foreach (var updated in allUpdatedAircraft)
            {
                count++;
                
                connection.FireMessage(new ServerToClient() {
                    ReplyToRequestId = replyToMessageId,
                    notify_aircraft_situation_updated = new() {
                        AircraftId = updated.Id,
                        Situation = CreateSituationMessage(updated)
                    }
                });
            }

            return count;
        }

        private int PushAircraftRemovedNotifications(
            IEnumerable<AircraftActor> allRemovedAircraft,
            IDeferredConnectionContext<ServerToClient> connection,
            ulong replyToMessageId)
        {
            var count = 0;
            
            foreach (var removed in allRemovedAircraft)
            {
                count++;
                connection.FireMessage(new ServerToClient() {
                    ReplyToRequestId = replyToMessageId,
                    notify_aircraft_removed = new() {
                        AircraftId = removed.Id
                    }
                });
            }

            return count;
        }

        private static AircraftMessage CreateAircraftMessage(AircraftActor aircraft)
        {
            return new AircraftMessage() {
                Id = aircraft.Id,
                AirlineIcao = aircraft.AirlineData?.Get().Icao.Value,
                ModelIcao = aircraft.TypeIcao,
                TailNo = aircraft.TailNo,
                situation = CreateSituationMessage(aircraft)
            };
        }
        
        private static AircraftMessage.Situation CreateSituationMessage(AircraftActor aircraft)
        {
            var situation = aircraft.GetCurrentSituation(forceRefresh: false);
            
            return new AircraftMessage.Situation() {
                Location = new() {
                    Lat = situation.Location.Lat,
                    Lon = situation.Location.Lon
                },
                AltitudeFeetMsl = situation.Altitude.Feet,
                Heading = situation.Heading.Degrees,
                GroundSpeedKt = situation.GroundSpeed.Knots,
                Pitch = situation.Pitch.Degrees,
                Roll = situation.Roll.Degrees,
                //TODO: add more
            };
        }
    }
}