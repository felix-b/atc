using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Atc.Data.Primitives;
using Atc.World;
using AtcProto;
using ProtoBuf.WellKnownTypes;
using Zero.Latency.Servers;

namespace Atc.Server
{
    public partial class WorldService
    {
        private readonly RuntimeWorld _world;
        private readonly ILogger _logger;

        public WorldService(RuntimeWorld world, ILogger logger)
        {
            _world = world;
            _logger = logger;
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

            void ObserveTrafficQuery(in QueryObservation<RuntimeAircraft> observation)
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

            static ServerToClient CreateReplyMessage(ClientToServer incoming, IObservableQuery<RuntimeAircraft> query)
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

        private void ValidateAuthentication(IDeferredConnectionContext<ServerToClient> connection)
        {
            if (!connection.Session.Has<ClientClaims.Authenticated>())
            {
                throw _logger.ConnectionWasNotAuthenticated(connection.Id);
            }
        }

        private int PushAircraftAddedNotifications(
            IEnumerable<RuntimeAircraft> allAddedAircraft,
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
            IEnumerable<RuntimeAircraft> allUpdatedAircraft,
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
            IEnumerable<RuntimeAircraft> allRemovedAircraft,
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

        private static AircraftMessage CreateAircraftMessage(RuntimeAircraft aircraft)
        {
            var state = aircraft.GetState();
            return new AircraftMessage() {
                Id = aircraft.Id,
                AirlineIcao = aircraft.AirlineData?.Get().Icao.Value,
                ModelIcao = aircraft.TypeIcao,
                TailNo = aircraft.TailNo,
                situation = CreateSituationMessage(aircraft)
            };
        }
        
        private static AircraftMessage.Situation CreateSituationMessage(RuntimeAircraft aircraft)
        {
            var state = aircraft.GetState();
            return new AircraftMessage.Situation() {
                Location = new() {
                    Lat = state.Location.Lat,
                    Lon = state.Location.Lon
                },
                AltitudeFeetMsl = state.Altitude.Feet,
                Heading = state.Heading.Degrees,
                Pitch = state.Pitch.Degrees,
                Roll = state.Roll.Degrees,
                //TODO: add more
            };
        }
    }
}