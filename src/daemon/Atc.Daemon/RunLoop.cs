using System.Collections.Immutable;
using Atc.Telemetry;

namespace Atc.Daemon;

public class RunLoop : IDisposable
{
    private ImmutableDictionary<string, FirEntry> _firById;
    private ImmutableHashSet<string> _leaderFirIds;
    private ImmutableDictionary<string, Thread> _firLoopThreadById;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IMyTelemetry _telemetry;
    private bool _disposed = false;

    public RunLoop(IEnumerable<FirEntry> firs, IEnumerable<string> leaderFirIds, IMyTelemetry telemetry)
    {
        _firById = firs.ToImmutableDictionary(fir => fir.Icao, fir => fir);
        _firLoopThreadById = firs.ToImmutableDictionary(
            fir => fir.Icao, 
            fir => CreateRunLoopThread(fir));
        _leaderFirIds = ImmutableHashSet<string>.Empty.Union(leaderFirIds);
        _telemetry = telemetry;
        
        Start();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        
        ShutDown(TimeSpan.FromSeconds(10));
        _cancellationTokenSource.Dispose();
    }

    private void Start()
    {
        foreach (var thread in _firLoopThreadById.Values)
        {
            thread.Start();
        }
    }
    
    private bool ShutDown(TimeSpan timeout)
    {
        _telemetry.InfoAllFirsShuttingDown();
        
        _cancellationTokenSource.Cancel();
        
        foreach (var kvp in _firLoopThreadById)
        {
            var thread = kvp.Value;
            if (!thread.Join(timeout))
            {
                _telemetry.WarningFailedTimelyShutdown(icao: kvp.Key, timeout);
                return false;
            }
        }
        
        _telemetry.InfoAllFirsShutdownTimely();
        return true;
    }

    private Thread CreateRunLoopThread(FirEntry fir)
    {
        return new Thread(state => {
            _telemetry.InfoFirLoopThreadStarted(icao: fir.Icao);
            
            try
            {
                RunFirLoop(fir);
                _telemetry.InfoFirLoopThreadCompleted(icao: fir.Icao);
            }
            catch (Exception e)
            {
                _telemetry.ErrorFirLoopThreadCrashed(icao: fir.Icao, exception: e);
            }
        });
    }

    private void RunFirLoop(FirEntry fir)
    {
        var silo = fir.Silo;
        //int iterationCount = 0;
        
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            //iterationCount++;
            WarnIfIdle();

            silo.BlockWhileIdle(_cancellationTokenSource.Token);
            silo.ExecuteReadyWorkItems();
        }

        void WarnIfIdle()
        {
            var nextWorkItemUtc = silo.NextWorkItemAtUtc;
            if (nextWorkItemUtc > silo.Environment.UtcNow.AddMinutes(30))
            {
                _telemetry.WarningFirIdle(icao: fir.Icao, nextWorkItemUtc);
            }
        }
    }
    
    [TelemetryName("RunLoop")]
    public interface IMyTelemetry : ITelemetry
    {
        void InfoFirLoopThreadStarted(string icao);
        void InfoFirLoopThreadCompleted(string icao);
        void ErrorFirLoopThreadCrashed(string icao, Exception exception);
        void WarningFirIdle(string icao, DateTime nextWorkItemUtc);
        void WarningFailedTimelyShutdown(string icao, TimeSpan timeout);
        void InfoAllFirsShutdownTimely();
        void InfoAllFirsShuttingDown();
    }
}
