using System.Collections.Immutable;
using Atc.Telemetry;
using Atc.Utilities;

namespace Atc.Daemon;

public class RunLoop : IDisposable
{
    private readonly ImmutableHashSet<string> _allFirIds;
    private WriteLocked<ImmutableHashSet<string>> _leaderFirIds;
    private WriteLocked<ImmutableDictionary<string, FirEntry>> _firById;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly FirSiloBuilder _siloBuilder;
    private readonly IMyTelemetry _telemetry;
    private bool _disposed = false;

    public RunLoop(
        FirSiloBuilder siloBuilder,
        IEnumerable<string> allFirIds, 
        IEnumerable<string> leaderFirIds, 
        IMyTelemetry telemetry)
    {
        _siloBuilder = siloBuilder;
        _allFirIds = ImmutableHashSet<string>.Empty.Union(allFirIds);
        _leaderFirIds = ImmutableHashSet<string>.Empty.Union(leaderFirIds);
        _firById = ImmutableDictionary<string, FirEntry>.Empty;
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
        var allLoopThreads = _allFirIds
            .Select(id => CreateRunLoopThread(id))
            .ToList();
        
        allLoopThreads.ForEach(thread => thread.Start());
    }
    
    private bool ShutDown(TimeSpan timeout)
    {
        _telemetry.InfoAllFirsShuttingDown();
        
        _cancellationTokenSource.Cancel();
        
        foreach (var kvp in _firById.Read())
        {
            var thread = kvp.Value.RunLoopThread!;
            if (!thread.Join(timeout))
            {
                _telemetry.WarningFailedTimelyShutdown(icao: kvp.Key, timeout);
                return false;
            }
        }
        
        _telemetry.InfoAllFirsShutdownTimely();
        return true;
    }

    private Thread CreateRunLoopThread(string firIcaoCode)
    {
        var thread =  new Thread(state => {
            _telemetry.VerboseFirLoopThreadStarted(icao: firIcaoCode);

            try
            {
                RunFirLoop(firIcaoCode);
                _telemetry.InfoFirLoopThreadCompleted(icao: firIcaoCode);
            }
            catch (OperationCanceledException)
            {
                _telemetry.InfoFirLoopThreadCompleted(icao: firIcaoCode);
            }
            catch (Exception e)
            {
                _telemetry.CriticalFirLoopThreadCrashed(icao: firIcaoCode, exception: e);
            }
        });

        thread.Name = $"FIR_{firIcaoCode}";
        thread.IsBackground = true;
        
        return thread;
    }

    private void RunFirLoop(string icao)
    {
        var entry = _siloBuilder.InitializeFir(icao);

        _telemetry.InfoFirInitialized(icao);
        _firById.Replace(old => old.SetItem(icao, entry));
        
        var silo = entry.Silo;
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
                _telemetry.WarningFirIdle(icao: icao, nextWorkItemUtc);
            }
        }
    }
    
    [TelemetryName("RunLoop")]
    public interface IMyTelemetry : ITelemetry
    {
        void VerboseFirLoopThreadStarted(string icao);
        void InfoFirLoopThreadCompleted(string icao);
        void CriticalFirLoopThreadCrashed(string icao, Exception exception);
        void WarningFirIdle(string icao, DateTime nextWorkItemUtc);
        void WarningFailedTimelyShutdown(string icao, TimeSpan timeout);
        void InfoAllFirsShutdownTimely();
        void InfoAllFirsShuttingDown();
        void InfoFirInitialized(string icao);
    }
}
