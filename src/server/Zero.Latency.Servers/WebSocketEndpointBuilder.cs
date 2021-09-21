using System;

namespace Zero.Latency.Servers
{
    public class WebSocketEndpointBuilder
    {
        public Step2<TMessageIn> ReceiveMessagesOfType<TMessageIn>()
            where TMessageIn : class
        {
            return new Step2<TMessageIn>();
        }

        public class Step2<TMessageIn>
            where TMessageIn : class
        {
            public Step3<TMessageIn, TDiscriminatorIn> WithDiscriminator<TDiscriminatorIn>(
                Func<TMessageIn, TDiscriminatorIn> extractDiscriminator)
                where TDiscriminatorIn : Enum
            {
                return new Step3<TMessageIn, TDiscriminatorIn>(extractDiscriminator);
            }
        }
        
        public class Step3<TMessageIn, TDiscriminatorIn>
            where TMessageIn : class
            where TDiscriminatorIn : Enum
        {
            private readonly Func<TMessageIn, TDiscriminatorIn> _extractDiscriminator;

            public Step3(Func<TMessageIn, TDiscriminatorIn> extractDiscriminator)
            {
                _extractDiscriminator = extractDiscriminator;
            }

            public Step4<TMessageIn, TDiscriminatorIn, TMessageOut> SendMessagesOfType<TMessageOut>()
                where TMessageOut : class
            {
                return new Step4<TMessageIn, TDiscriminatorIn, TMessageOut>(_extractDiscriminator);
            }
        }

        public class Step4<TMessageIn, TDiscriminatorIn, TMessageOut>
            where TMessageIn : class
            where TMessageOut : class
            where TDiscriminatorIn : Enum
        {
            private readonly Func<TMessageIn, TDiscriminatorIn> _extractDiscriminator;

            public Step4(Func<TMessageIn, TDiscriminatorIn> extractDiscriminator)
            {
                _extractDiscriminator = extractDiscriminator;
            }

            public Step5<TMessageIn, TDiscriminatorIn, TMessageOut> ListenOn(int portNumber = 80, string urlPath = "/ws")
            {
                return new Step5<TMessageIn, TDiscriminatorIn, TMessageOut>(_extractDiscriminator, portNumber, urlPath);
            }
        }

        public class Step5<TMessageIn, TDiscriminatorIn, TMessageOut>
            where TMessageIn : class
            where TMessageOut : class
            where TDiscriminatorIn : Enum
        {
            private readonly Func<TMessageIn, TDiscriminatorIn> _extractDiscriminator;
            private int _portNumber;
            private string _urlPath;

            public Step5(Func<TMessageIn, TDiscriminatorIn> extractDiscriminator, int portNumber, string urlPath)
            {
                _extractDiscriminator = extractDiscriminator;
                _portNumber = portNumber;
                _urlPath = urlPath;
            }

            public Step6<TMessageIn, TDiscriminatorIn, TMessageOut> BindToServiceInstance(object serviceInstance)
            {
                return new Step6<TMessageIn, TDiscriminatorIn, TMessageOut>(
                    _extractDiscriminator, _portNumber, _urlPath, serviceInstance);
            }
        }

        public class Step6<TMessageIn, TDiscriminatorIn, TMessageOut>
            where TMessageIn : class
            where TMessageOut : class
            where TDiscriminatorIn : Enum
        {
            private readonly Func<TMessageIn, TDiscriminatorIn> _extractDiscriminator;
            private int _portNumber;
            private string _urlPath;
            private object _serviceInstance;

            public Step6(Func<TMessageIn, TDiscriminatorIn> extractDiscriminator, int portNumber, string urlPath, object serviceInstance)
            {
                _extractDiscriminator = extractDiscriminator;
                _portNumber = portNumber;
                _urlPath = urlPath;
                _serviceInstance = serviceInstance;
            }

            public WebSocketEndpoint Create(IEndpointLogger logger, out IServiceTaskSynchronizer taskSynchronizer)
            {
                var operationDispatcher2 = new MethodInvocationOperationDispatcher<TMessageIn, TMessageOut, TDiscriminatorIn>(
                    _serviceInstance,
                    _extractDiscriminator,
                    logger
                );
                var operationDispatcher1 = new QueueOperationDispatcher<TMessageIn, TMessageOut>(
                    outputThreadCount: 1, 
                    operationDispatcher2,
                    logger
                );
                taskSynchronizer = operationDispatcher1;
                
                var messageSerializer = new ProtobufEnvelopeSerializer<TMessageIn>(logger);
                var connectionManager = new SocketConnectionManager(messageSerializer, operationDispatcher1, logger);
            
                var endpoint = new WebSocketEndpoint(_portNumber, _urlPath, connectionManager, logger);
                return endpoint;
            }
        }
    }
}