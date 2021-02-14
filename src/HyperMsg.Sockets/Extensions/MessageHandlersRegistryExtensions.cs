using System;
using System.Net.Sockets;

namespace HyperMsg.Sockets.Extensions
{
    public static class MessageHandlersRegistryExtensions
    {
        public static IDisposable RegisterAcceptedSocketHandler(this IMessageHandlersRegistry handlersRegistry, Func<Socket, bool> acceptedSocketHandler)
        {
            return handlersRegistry.RegisterHandler<AcceptedSocket>(accceptedSocket =>
            {
                accceptedSocket.IsAquired = acceptedSocketHandler.Invoke(accceptedSocket.Socket);
            });
        }
    }
}
