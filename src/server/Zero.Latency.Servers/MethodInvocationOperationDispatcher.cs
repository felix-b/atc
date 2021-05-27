using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Zero.Latency.Servers
{
    public class MethodInvocationOperationDispatcher<TIn, TOut, TDiscriminatorIn> : IOperationDispatcher<object, object>
        where TIn : class
        where TOut : class
        where TDiscriminatorIn : Enum
    {
        private delegate ValueTask OperationMethod(IConnectionContext<TOut> connection, TIn message); 

        private readonly object _serviceInstance;
        private readonly Func<TIn, TDiscriminatorIn> _extractDiscriminator;
        private readonly IReadOnlyDictionary<TDiscriminatorIn, OperationMethod> _methodByDiscriminator;

        public MethodInvocationOperationDispatcher(object serviceInstance, Func<TIn, TDiscriminatorIn> extractDiscriminator)
        {
            _extractDiscriminator = extractDiscriminator;
            _serviceInstance = serviceInstance;
            _methodByDiscriminator = BuildMethodInvocationMap(serviceInstance);
        }

        public async ValueTask DisposeAsync()
        {
            if (_serviceInstance is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (_serviceInstance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        
        public ValueTask DispatchOperation(IConnectionContext<object> connection, object message)
        {
            var typedMessage = (TIn)message;
            var typedConnection = new TypedConnectionContext(connection);
            var discriminator = _extractDiscriminator(typedMessage);

            if (_methodByDiscriminator.TryGetValue(discriminator, out var operation))
            {
                try
                {
                    //TODO: support methods that return ValueTask
                    return operation(typedConnection, typedMessage);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: OPERATION FAILED! {e}");
                    return ValueTask.FromException(e);
                }
            }

            throw new KeyNotFoundException(
                $"No operation method found for incoming message with discriminator value '{discriminator}'.");
        }

        private static IReadOnlyDictionary<TDiscriminatorIn, OperationMethod> BuildMethodInvocationMap(object serviceInstance)
        {
            var result = serviceInstance
                .GetType()
                .GetMethods()
                .Where(HasCompatibleSignature) 
                .Select(m => new {
                    Attribute = m.GetCustomAttribute<PayloadCaseAttribute>(), 
                    Method = m
                })
                .Where(tuple => tuple.Attribute != null)
                .Select(tuple => new {
                    PayloadCase = (TDiscriminatorIn) tuple.Attribute!.Discriminator,
                    Delegate = tuple.Method.CreateDelegate<OperationMethod>(target: serviceInstance) //TODO: compare performance with IL emit
                })
                .ToDictionary(
                    tuple => tuple.PayloadCase, 
                    tuple => tuple.Delegate
                );
                
            return result;

            bool HasCompatibleSignature(MethodInfo info)
            {
                if (info.ReturnType != typeof(ValueTask))
                {
                    return false;
                }

                var parameters = info.GetParameters();
                return (
                    parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(IConnectionContext<TOut>) &&
                    parameters[1].ParameterType == typeof(TIn));
            }
        }
        
        private class TypedConnectionContext : IConnectionContext<TOut>
        {
            private readonly IConnectionContext<object> _inner;

            public TypedConnectionContext(IConnectionContext<object> inner)
            {
                _inner = inner;
            }

            public ValueTask SendMessage(TOut message)
            {
                return _inner.SendMessage(message);
            }

            public void RegisterObserver(IObserverSubscription observer)
            {
                _inner.RegisterObserver(observer);
            }

            public ValueTask CloseConnection()
            {
                return _inner.CloseConnection();
            }

            public long Id => _inner.Id;

            public bool IsActive => _inner.IsActive;

            public CancellationToken Cancellation => _inner.Cancellation;
        }
    }
}
