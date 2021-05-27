using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf.WellKnownTypes;
using Zero.Latency.Servers;

namespace Atc.World
{
    public class RuntimeClock
    {
        private readonly object _syncRoot = new();
        private readonly TimeSpan _interval;
        private readonly IServiceTaskSynchronizer _synchronizer;
        private readonly RuntimeWorld _target;
        private TimeSpan _startTimestamp = TimeSpan.Zero;
        private Task? _task = null;
        private CancellationTokenSource? _stopping = null;

        public RuntimeClock(TimeSpan interval, IServiceTaskSynchronizer synchronizer, RuntimeWorld target)
        {
            _interval = interval;
            _synchronizer = synchronizer;
            _target = target;
        }

        public void Start()
        {
            lock (_syncRoot)
            {
                if (_task is null)
                {
                    _stopping = new CancellationTokenSource();
                    _startTimestamp = SystemTimestampNow;
                    _task = RunTickLoop();
                }
            }
        }

        public void Stop()
        {
            lock (_syncRoot)
            {
                _stopping?.Cancel();

                try
                {
                    _task?.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"WARNING: RUNTIME CLOCK TICK LOOP CRASHED: {e}");
                }
                
                _task = null;
                _stopping = null;
            }
        }

        private async Task RunTickLoop()
        {
            try
            {
                var lastTickTimestamp = _startTimestamp;
                
                while (_stopping != null && !_stopping.IsCancellationRequested)
                {
                    await Task.Delay(_interval, _stopping.Token);
                
                    _synchronizer.SubmitTask(() => {
                        var now = SystemTimestampNow;
                        var elapsedSinceLastTick = now - lastTickTimestamp;
                        lastTickTimestamp = now;

                        _target.ProgressBy(elapsedSinceLastTick);
                    });
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"RUNTIME CLOCK TICK LOOP: STOP REQUESTED, EXITING");
            }
            catch (Exception e)
            {
                Console.WriteLine($"RUNTIME CLOCK TICK LOOP CRASHED! {e}");
            }
        }
        
        public const long TicksPerMillisecond = 10000;
        public const long TicksPerSecond = TicksPerMillisecond * 1000;
        public static readonly double TickFrequency = (double)TicksPerSecond / Stopwatch.Frequency;

        public static TimeSpan SystemTimestampNow => ToTimeSpan(Stopwatch.GetTimestamp());

        private static TimeSpan ToTimeSpan(long timestamp)
        {
            var timeSpanTicks = unchecked((long)(timestamp * TickFrequency));
            return new TimeSpan(timeSpanTicks);
        }
    }
}
