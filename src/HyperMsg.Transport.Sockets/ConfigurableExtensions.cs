using System.Net;

namespace HyperMsg.Transport.Sockets
{
    public static class ConfigurableExtensions
    {
        public static void AddSocketTransceiver(this IConfigurable configurable, EndPoint endpoint)
        {
            configurable.AddInitializer(provider =>
            {
                var context = provider.GetRequiredService<IBufferContext>();
                var socket = CreateDefaultSocket(endpoint);
                context.AttachSocket(socket);
                configurable.AddService<IPort>(socket);
                configurable.AddService<IPort<EndPoint>>(socket);
                configurable.AddTransportCommandHandler(socket);
                configurable.AddDataTransmissionCommandHandler(socket);
            });
        }

        private static SocketProxy CreateDefaultSocket(EndPoint endPoint)
        {
            var socket = SocketFactory.CreateTcpSocket();

            return new SocketProxy(socket, endPoint);
        }
    }
}