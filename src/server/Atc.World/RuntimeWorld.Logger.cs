using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atc.Data.Primitives;
using Zero.Doubt.Logging;
using Zero.Latency.Servers;

namespace Atc.World
{
    public partial class RuntimeWorld
    {
        public interface ILogger
        {
            void TrafficQueryObserverCreated(double minLat, double minLon, double maxLat, double maxLon);
            void TrafficQueryObserverDisposing(double minLat, double minLon, double maxLat, double maxLon);
            LogWriter.LogSpan ProgressBy(int deltaMs, int newTimestampMs, ulong newTickCount);
            LogWriter.LogSpan ProgressByAircraft(uint aircraftId);
            LogWriter.LogSpan StateOperationLifecycle(string originator);
            LogWriter.LogSpan ObserverCheckingForUpdates(string observerName);
            void RegisteringObserver(string observerName);
            void FailedToFindRadioAether(string fromStation, int khz, double lat, double lon, float feet);
            void FoundRadioAether(string fromStation, string groundStation, int khz, double lat, double lon, float feet);
        }
    }
}