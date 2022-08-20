using Atc.Grains;
using Autofac;

namespace Atc.Daemon;

public class AtcdSiloDependencyBuilder : ISiloDependencyBuilder
{
    private readonly ContainerBuilder _buidler = new();
    
    public void AddSingleton<T>(T singletonInstance) where T : class
    {
        _buidler.RegisterInstance<T>(singletonInstance);
    }

    public void AddTransient<T>(Func<T> transientFactory) where T : class
    {
        _buidler.Register<T>(ctx => transientFactory()).InstancePerDependency();
    }

    public ISiloDependencyContext GetContext()
    {
        return new AtcdSiloDependencyContext(_buidler.Build());
    }
}

public class AtcdSiloDependencyContext : ISiloDependencyContext
{
    private readonly IComponentContext _context;

    public AtcdSiloDependencyContext(IComponentContext context)
    {
        _context = context;
    }

    public T Resolve<T>() where T : class
    {
        return _context.Resolve<T>();
    }
}
