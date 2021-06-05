using System;
using System.Security;
using Zero.Doubt.Logging;

namespace Atc.Server
{
    public partial class WorldService
    {
        public interface ILogger
        {
            void ConnectRequestReceived(long connectionId, string token);
            void ConnectionAuthenticated(long connectionId);
            void ConnectionAuthenticationFailed(long connectionId, string reason);
            SecurityException ConnectionWasNotAuthenticated(long connectionId);
            void SentTrafficQueryResults(long connectionId, int count);
            void TrafficQueryObserverConnectionNotActive(long connectionId);
            LogWriter.LogSpan TrafficQueryObserverPushingNotifications(long connectionId);
            void TrafficQueryObserverCompleted(long connectionId, int updated, int added, int removed);
        }

        public class Logger : ILogger
        {
            private static readonly string _s_connectRequestReceived = "WorldService.ConnectRequestReceived";
            private static readonly string _s_connectionId = "connectionId";
            private static readonly string _s_token = "token";
            private static readonly string _s_connectionAuthenticated = "WorldService.ConnectionAuthenticated";
            private static readonly string _s_connectionAuthenticationFailed = "WorldService.ConnectionAuthenticationFailed";
            private static readonly string _s_reason = "reason";
            private static readonly string _s_connectionWasNotAuthenticated = "WorldService.ConnectionWasNotAuthenticated";
            private static readonly string _s_sentTrafficQueryResults = "WorldService.SentTrafficQueryResults";
            private static readonly string _s_count = "count";

            private static readonly string _s_trafficQueryObserverConnectionNotActive =
                "WorldService.TrafficQueryObserverConnectionNotActive";

            private static readonly string _s_trafficQueryObserverPushingNotifications =
                "WorldService.TrafficQueryObserverPushingNotifications";

            private static readonly string _s_trafficQueryObserverCompleted = "WorldService.TrafficQueryObserverCompleted";
            private static readonly string _s_updated = "updated";
            private static readonly string _s_added = "added";
            private static readonly string _s_removed = "removed";

            private readonly LogWriter _writer;

            public Logger(LogWriter writer)
            {
                _writer = writer;
            }

            public void ConnectRequestReceived(long connectionId, string token)
            {
                _writer.Message(
                    _s_connectRequestReceived,
                    LogLevel.Debug,
                    (_s_connectionId, connectionId),
                    (_s_token, token));
            }

            public void ConnectionAuthenticated(long connectionId)
            {
                _writer.Message(
                    _s_connectionAuthenticated,
                    LogLevel.Info,
                    (_s_connectionId, connectionId));
            }

            public void ConnectionAuthenticationFailed(long connectionId, string reason)
            {
                _writer.Message(
                    _s_connectionAuthenticationFailed,
                    LogLevel.Debug,
                    (_s_connectionId, connectionId),
                    (_s_reason, reason));
            }

            public SecurityException ConnectionWasNotAuthenticated(long connectionId)
            {
                _writer.Message(
                    _s_connectionWasNotAuthenticated,
                    LogLevel.Error,
                    (_s_connectionId, connectionId));

                return new SecurityException(
                    $"{_s_connectionWasNotAuthenticated}: {_s_connectionId}={connectionId}");
            }

            public void SentTrafficQueryResults(long connectionId, int count)
            {
                _writer.Message(
                    _s_sentTrafficQueryResults,
                    LogLevel.Debug,
                    (_s_connectionId, connectionId),
                    (_s_count, count));
            }

            public void TrafficQueryObserverConnectionNotActive(long connectionId)
            {
                _writer.Message(
                    _s_trafficQueryObserverConnectionNotActive,
                    LogLevel.Error,
                    (_s_connectionId, connectionId));
            }

            public LogWriter.LogSpan TrafficQueryObserverPushingNotifications(long connectionId)
            {
                return _writer.Span(
                    _s_trafficQueryObserverPushingNotifications,
                    LogLevel.Debug,
                    (_s_connectionId, connectionId));
            }

            public void TrafficQueryObserverCompleted(long connectionId, int updated, int added, int removed)
            {
                _writer.Message(
                    _s_trafficQueryObserverCompleted,
                    LogLevel.Debug,
                    (_s_connectionId, connectionId),
                    (_s_updated, updated),
                    (_s_added, added),
                    (_s_removed, removed));
            }
        }
    }
}
