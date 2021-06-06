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
            void TrafficQueryObserverCreated(long connectionId, string registrationKey, double minLat, double minLon, double maxLat, double maxLon);
            void TrafficQueryObserverCanceled(long connectionId, string registrationKey);
        }
    }
}
