using System;
using Zero.Doubt.Logging;

namespace Atc.World
{
    public partial class WorldActor
    {
        private struct OperationLifecycle : IDisposable
        {
            private readonly WorldActor _target;
            private LogWriter.LogSpan _logSpan;

            public OperationLifecycle(WorldActor target, string originator)
            {
                _target = target;
                _logSpan = _target.Logger.StateOperationLifecycle(originator);
            }

            public void Dispose()
            {
                try
                {
                    foreach (var observer in _target._observers)
                    {
                        using var observerLogSpan = _target.Logger.ObserverCheckingForUpdates(observer.Name);

                        try
                        {
                            observer.CheckForUpdates();
                        }
                        catch (Exception e)
                        {
                            observerLogSpan.Fail(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logSpan.Fail(e);
                }
                finally
                {
                    _logSpan.Dispose();
                }
            }
        }
    }
}
