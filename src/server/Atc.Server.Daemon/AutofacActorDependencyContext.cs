using Autofac;
using Zero.Loss.Actors;

namespace Atc.Server.Daemon
{
    public class AutofacActorDependencyContext : IActorDependencyContext
    {
        private readonly IComponentContext _container;

        public AutofacActorDependencyContext(IComponentContext container)
        {
            _container = container;
        }

        public T Resolve<T>() where T : class
        {
            return _container.Resolve<T>();
        }
    }
}
