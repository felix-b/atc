namespace Atc.World
{
    public interface IWorldObserver
    {
        void CheckForUpdates();
        string Name { get; }
    }
}
