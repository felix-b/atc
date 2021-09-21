using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Zero.Latency.Servers
{
    public class MethodInvocationOperationDispatcher<TEnvelopeIn, TEnvelopeOut, TPayloadCaseIn> : IDeferredOperationDispatcher<TEnvelopeIn, TEnvelopeOut>
        where TEnvelopeIn : class
        where TEnvelopeOut : class
        where TPayloadCaseIn : Enum
    {
        private delegate void OperationMethod(IDeferredConnectionContext<TEnvelopeOut> connection, TEnvelopeIn envelope); 

        private readonly object _serviceInstance;
        private readonly Func<TEnvelopeIn, TPayloadCaseIn> _extractPayloadCase;
        private readonly IEndpointLogger _logger;
        private readonly IReadOnlyDictionary<TPayloadCaseIn, OperationMethod> _methodByPayloadCase;

        public MethodInvocationOperationDispatcher(
            object serviceInstance, 
            Func<TEnvelopeIn, TPayloadCaseIn> extractPayloadCase,
            IEndpointLogger logger)
        {
            _serviceInstance = serviceInstance;
            _extractPayloadCase = extractPayloadCase;
            _logger = logger;
            _methodByPayloadCase = BuildMethodInvocationMap(serviceInstance);
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
        
        public void DispatchOperation(IDeferredConnectionContext<TEnvelopeOut> connection, TEnvelopeIn message)
        {
            //TODO: how are errors propagated back?
            
            var payloadCase = _extractPayloadCase(message);

            if (_methodByPayloadCase.TryGetValue(payloadCase, out var method))
            {
                try
                {
                    method(connection, message);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: OPERATION FAILED! {e}"); //TODO: how to propagate back?
                }
            }
            else
            {
                throw new KeyNotFoundException(
                    $"No operation method found for incoming message with discriminator value '{payloadCase}'.");
            }
        }

        private static IReadOnlyDictionary<TPayloadCaseIn, OperationMethod> BuildMethodInvocationMap(object serviceInstance)
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
                    PayloadCase = (TPayloadCaseIn) tuple.Attribute!.Discriminator,
                    Delegate = tuple.Method.CreateDelegate<OperationMethod>(target: serviceInstance) //TODO: compare performance with IL emit
                })
                .ToDictionary(
                    tuple => tuple.PayloadCase, 
                    tuple => tuple.Delegate
                );
                
            return result;

            bool HasCompatibleSignature(MethodInfo info)
            {
                var parameters = info.GetParameters();

                return (
                    info.ReturnType == typeof(void) &&
                    parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(IDeferredConnectionContext<TEnvelopeOut>) &&
                    parameters[1].ParameterType == typeof(TEnvelopeIn));
            }
        }
    }
}
