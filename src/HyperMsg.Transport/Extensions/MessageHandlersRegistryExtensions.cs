using System;

namespace HyperMsg.Transport.Extensions
{
    public static class MessageHandlersRegistryExtensions
    {
        public static IDisposable RegisterSetEndpointHandler<TEndpoint>(this IMessageHandlersRegistry handlersRegistry, Action<TEndpoint> messageHandler) => 
            handlersRegistry.RegisterHandler<SetEndpoint<TEndpoint>>(se => messageHandler.Invoke(se.Endpoint));

        public static IDisposable RegisterSetEndpointHandler<TEndpoint>(this IMessageHandlersRegistry handlersRegistry, AsyncAction<TEndpoint> messageHandler) => 
            handlersRegistry.RegisterHandler<SetEndpoint<TEndpoint>>((se, token) => messageHandler.Invoke(se.Endpoint, token));
    }
}
