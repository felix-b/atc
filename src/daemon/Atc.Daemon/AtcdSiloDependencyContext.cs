using Atc.Grains;
using Autofac;

namespace Atc.Daemon;

public class AtcdSiloDependencyBuilder : ISiloDependencyBuilder
{
    private readonly ContainerBuilder _buidler = new();
    private AtcdSiloDependencyContext? _context = null;
    
    public void AddSingleton<T>(T singletonInstance) where T : class
    {
        ValidateMutable();
        _buidler.RegisterInstance<T>(singletonInstance);
    }

    public void AddTransient<T>(Func<ISiloDependencyContext, T> transientFactory) where T : class
    {
        ValidateMutable();
        _buidler.Register<T>(ctx => transientFactory(_context!)).InstancePerDependency();
    }

    public ISiloDependencyContext GetContext()
    {
        if (_context == null)
        {
            _context = new AtcdSiloDependencyContext(_buidler.Build());
        }

        return _context;
    }

    private void ValidateMutable()
    {
        if (_context != null)
        {
            throw new InvalidOperationException("DI container was already built");
        }
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
