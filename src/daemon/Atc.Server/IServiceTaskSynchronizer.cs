namespace Atc.Server;

public interface IServiceTaskSynchronizer
{
    void SubmitTask(Action callback);
}
