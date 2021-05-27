using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zero.Latency.Servers
{
    public interface IServiceTaskSynchronizer
    {
        void SubmitTask(Action callback);
    }
}
