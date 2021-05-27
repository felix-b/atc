using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Zero.Latency.Servers
{
    public static class WriteLockedExtensions
    {
        public static async ValueTask DisposeAllAsync<T>(this WriteLocked<ImmutableList<T>> source) 
            where T : class, IAsyncDisposable
        {
            var snapshot = source.Exchange(list => list.Clear());
            var disposeTasks = snapshot.AsEnumerable().Select(
                disposable => disposable.DisposeAsync()
            ).ToArray();
                
            for (int i = 0 ; i < disposeTasks.Length ; i++)
            {
                await disposeTasks[i];
            }
        }
    }
}
