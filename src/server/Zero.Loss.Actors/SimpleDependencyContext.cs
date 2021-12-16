﻿using System;
using System.Collections.Generic;

namespace Zero.Loss.Actors
{
    public class SimpleDependencyContext : IActorDependencyContext
    {
        private readonly Dictionary<Type, Delegate> _factoryByType = new();

        public SimpleDependencyContext()
        {
        }

        public bool Has<T>() where T : class
        {
            return _factoryByType.ContainsKey(typeof(T));
        }

        public SimpleDependencyContext WithTransient<T>(Func<T> factory) where T : class
        {
            if (!Has<T>())
            {
                _factoryByType.Add(typeof(T), factory);
            }

            return this;
        }

        public SimpleDependencyContext WithSingleton<T>(T instance, bool replace = false) where T : class
        {
            if (!Has<T>() || replace)
            {
                Func<T> singletonGetter = () => instance;
                _factoryByType[typeof(T)] = singletonGetter;
            }
            
            return this;
        }

        public SimpleDependencyContext WithSingleton<T, TImpl>() //TODO: test!
            where T : class
            where TImpl : class, T, new()
        {
            return WithSingleton<T, TImpl>(ctx => new TImpl());
        }
        
        public SimpleDependencyContext WithSingleton<T, TImpl>(Func<IActorDependencyContext, T> factory) //TODO: test!
            where T : class
            where TImpl : class, T
        {
            if (!Has<T>())
            {
                T? singletonInstance = null;
                Func<T> singletonGetter = () => {
                    if (singletonInstance == null)
                    {
                        singletonInstance = factory(this);
                    }
                    return singletonInstance;
                };
                _factoryByType.Add(typeof(T), singletonGetter);
            }
            
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