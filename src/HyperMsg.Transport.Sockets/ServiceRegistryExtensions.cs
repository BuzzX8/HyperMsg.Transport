using System.Net;
using System.Net.Sockets;

namespace HyperMsg.Transport.Sockets
{
    public static class ServiceRegistryExtensions
    {
        public static void AddSocketTransport(this IServiceRegistry serviceRegistry, EndPoint endpoint)
        {
            var socket = SocketFactory.CreateTcpSocket();

            serviceRegistry.AddTransportCommandHandler(new SocketPortAdapter(socket, endpoint));
            serviceRegistry.AddDataTransmissionCommandHandler(new SocketTransceivingProxy(socket));
            serviceRegistry.AddSocketDataObserver(socket);
        }

        private static void AddSocketDataObserver(this IServiceRegistry serviceRegistry, Socket socket)
        {
            serviceRegistry.AddService(provider =>
            {
                var messagingContext = provider.GetRequiredService<IMessagingContext>();
                var bufferContext = provider.GetRequiredService<IBufferContext>();
                return new SocketDataObserver(messagingContext, bufferContext.ReceivingBuffer, socket);
            });
        }
    }
}