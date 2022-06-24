namespace Atc.Grains.Tests.Samples;

public class TestSiloDependencyContext : ISiloDependencyBuilder, ISiloDependencyContext
{
    private readonly Dictionary<Type, object> _singletonInstanceByType = new();
    private readonly Dictionary<Type, Delegate> _transientFactoryByType = new();

    public void AddSingleton<T>(T singletonInstance)
    {
        _singletonInstanceByType[typeof(T)] = singletonInstance!;
    }

    public void AddTransient<T>(Func<T> factory)
    {
        _transientFactoryByType[typeof(T)] = factory;
    }

    public ISiloDependencyContext GetContext()
    {
        return this;
    }

    T ISiloDependencyContext.Resolve<T>() where T : class
    {
        if (_singletonInstanceByType.TryGetValue(typeof(T), out var untypedInstance))
        {
            return (T) untypedInstance;
        }

        var factory = (Func<T>) _transientFactoryByType[typeof(T)];
        return factory();
    }
}
