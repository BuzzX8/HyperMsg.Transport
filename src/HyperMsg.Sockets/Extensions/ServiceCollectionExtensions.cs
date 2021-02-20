using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;

namespace HyperMsg.Sockets.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private static IServiceCollection AddSocketServices(this IServiceCollection services, Func<EndPoint> endpointProvider = null)
        {
            var socket = SocketFactory.CreateTcpSocket();
            return services.AddHostedService(provider =>
            {
                var context = provider.GetRequiredService<IMessagingContext>();

                return new SocketConnectionService(context, socket, endpointProvider ?? DefaultProvider);
            })
            .AddHostedService(provider =>
            {
                var bufferContext = provider.GetRequiredService<IBufferContext>();
                var messagingContext = provider.GetRequiredService<IMessagingContext>();
                return new SocketDataTransferService(socket, bufferContext.ReceivingBuffer, messagingContext);
            });
        }

        private static EndPoint DefaultProvider() => throw new InvalidOperationException("End point does not provided.");

        public static IServiceCollection AddSocketConnection(this IServiceCollection services, string hostName, int port)
        {
            return services.AddSocketServices(() =>
            {
                var addresses = Dns.GetHostAddresses(hostName);
                return new IPEndPoint(addresses[0], port);
            });
        }

        public static IServiceCollection AddSocketConnection(this IServiceCollection services, IPAddress address, int port) => services.AddSocketServices(() => new IPEndPoint(address, port));

        public static IServiceCollection AddLocalSocketConnection(this IServiceCollection services, int port) => services.AddSocketConnection(IPAddress.Loopback, port);

        public static IServiceCollection AddSocketConnectionListener(this IServiceCollection services, EndPoint listeningEndpoint, int backlog = 1)
        {
            return services.AddHostedService(provider =>
            {
                var messagingContext = provider.GetRequiredService<IMessagingContext>();
                
                return new SocketConnectionListeneningService(messagingContext, SocketFactory.CreateTcpSocket, listeningEndpoint, backlog);
            });
        }
    }
}