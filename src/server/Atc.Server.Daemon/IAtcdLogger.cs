namespace Atc.Server.Daemon
{
    public interface IAtcdLogger
    {
        void DaemonStarting();
        
        void DaemonStarted(string worldServiceUrl);
        
        void DaemonStopping();
        
        void DaemonStopped();
    }
}
