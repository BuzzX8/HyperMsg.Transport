using System.Net;
using System.Net.Sockets;

namespace HyperMsg.Transport.Sockets
{
    public static class ConfigurableExtensions
    {
        public static void AddSocketTransport(this IConfigurable configurable, EndPoint endpoint)
        {
            var socket = SocketFactory.CreateTcpSocket();

            configurable.AddTransportCommandHandler(new SocketPortAdapter(socket, endpoint));
            configurable.AddDataTransmissionCommandHandler(new SocketTransceivingProxy(socket));
            configurable.AddSocketDataObserver(socket);
        }

        private static void AddSocketDataObserver(this IConfigurable configurable, Socket socket)
        {
            configurable.AddService(provider =>
            {
                var messagingContext = provider.GetRequiredService<IMessagingContext>();
                var bufferContext = provider.GetRequiredService<IBufferContext>();
                return new SocketDataObserver(messagingContext, bufferContext.ReceivingBuffer, socket);
            });
        }
    }
}