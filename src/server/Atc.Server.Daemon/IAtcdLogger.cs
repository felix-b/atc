namespace Atc.Server.Daemon
{
    public interface IAtcdLogger
    {
        void DaemonStarting();

        void LoadingCache(string filePath);
        
        void CacheLoaded();

        void DaemonStarted(string worldServiceUrl);
        
        void DaemonStopping();
        
        void DaemonStopped();
    }
}
