namespace Atc.Speech.AzurePlugin;

public class ExclusiveLock
{
    private readonly string _name;
    private readonly SemaphoreSlim _semaphore;

    public ExclusiveLock(string name)
    {
        _name = name;
        _semaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);
    }

    public IDisposable Acquire(TimeSpan timeout)
    {
        if (!_semaphore.Wait(timeout))
        {
            throw new TimeoutException($"Timed out acquiring lock on '{_name}.");
        }

        return new Scope(_semaphore);
    }

    private class Scope : IDisposable
    {
        private int _disposeCount = 0;
        private readonly SemaphoreSlim _semaphore;

        public Scope(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCount) == 1)
            {
                _semaphore.Release();
            }
        }
    }
}
