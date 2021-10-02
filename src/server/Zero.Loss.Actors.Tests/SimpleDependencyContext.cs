using System;
using System.Collections.Generic;

namespace Zero.Loss.Actors.Tests
{
    public class SimpleDependencyContext : IActorDependencyContext
    {
        private readonly Dictionary<Type, Delegate> _factoryByType = new();

        public SimpleDependencyContext()
        {
        }

        public SimpleDependencyContext WithTransient<T>(Func<T> factory) where T : class
        {
            _factoryByType.Add(typeof(T), factory);
            return this;
        }

        public SimpleDependencyContext WithSingleton<T>(T instance) where T : class
        {
            Func<T> singletonGetter = () => instance;
            _factoryByType.Add(typeof(T), singletonGetter);
            return this;
        }

        T IActorDependencyContext.Resolve<T>() where T : class
        {
            if (_factoryByType.TryGetValue(typeof(T), out var untypedFactory))
            {
                var typedFactory = (Func<T>) untypedFactory;
                return typedFactory();
            }

            throw new KeyNotFoundException($"Service for type '{typeof(T).Name}' was not registered");
        }

        public static SimpleDependencyContext NewEmpty()
        {
            return new SimpleDependencyContext();
        }

        public static SimpleDependencyContext NewWithStore(IStateStore store)
        {
            return new SimpleDependencyContext().WithSingleton<IStateStore>(store);
        }
    }
}