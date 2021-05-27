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
    public class WorldService
    {
        private readonly RuntimeWorld _world;
        private readonly IWorldServiceLogger _logger;

        public WorldService(RuntimeWorld world, IWorldServiceLogger logger)
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
            connection.RegisterObserver(subscription);

            int count = 0;
            foreach (var result in query.GetResults())
            {
                count++;
                connection.FireMessage(new ServerToClient() {
                    ReplyToRequestId = message.Id,
                    notify_aircraft_created = new() {
                        Aircraft = CreateAircraftMessage(result),
                    }
                });
            }

            _logger.SentTrafficQueryResults(connection.Id, count);

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
                
                var state = updated.GetState();
                connection.FireMessage(new ServerToClient() {
                    ReplyToRequestId = replyToMessageId,
                    notify_aircraft_situation_updated = new() {
                        AirctaftId = state.Id,
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
                        AirctaftId = removed.Id
                    }
                });
            }

            return count;
        }

        private AircraftMessage CreateAircraftMessage(RuntimeAircraft aircraft)
        {
            var state = aircraft.GetState();
            return new AircraftMessage() {
                Id = aircraft.Id,
                AirlineIcao = aircraft.AirlineData?.Get().Icao.Value,
                ModelIcao = state.TypeIcao,
                TailNo = state.TailNo,
                situation = CreateSituationMessage(aircraft)
            };
        }
        
        private AircraftMessage.Situation CreateSituationMessage(RuntimeAircraft aircraft)
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