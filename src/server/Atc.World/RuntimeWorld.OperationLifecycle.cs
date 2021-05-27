using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.World.Redux;
using Zero.Latency.Servers;

namespace Atc.World
{
    public partial class RuntimeWorld
    {
        private struct OperationLifecycle : IDisposable
        {
            private readonly RuntimeWorld _target;

            public OperationLifecycle(RuntimeWorld target)
            {
                _target = target;
            }

            public void Dispose()
            {
                foreach (var observer in _target._observers)
                {
                    observer.CheckForUpdates();
                }
            }
        }
    }
}
