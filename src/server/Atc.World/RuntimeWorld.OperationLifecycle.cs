using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.World.Redux;
using Zero.Doubt.Logging;
using Zero.Latency.Servers;

namespace Atc.World
{
    public partial class RuntimeWorld
    {
        private struct OperationLifecycle : IDisposable
        {
            private readonly RuntimeWorld _target;
            private LogWriter.LogSpan _logSpan;

            public OperationLifecycle(RuntimeWorld target, string originator)
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
