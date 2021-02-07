using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;

namespace HyperMsg.Sockets
{
    public static class ServiceCollectionExtensions
    {
        private static IServiceCollection AddSocketServices(this IServiceCollection services, Func<EndPoint> endpointProvider)
        {
            var socket = SocketFactory.CreateTcpSocket();
            return services.AddSingleton(provider =>
            {
                var context = provider.GetRequiredService<IMessagingContext>();

                return new SocketConnectionService(context, socket, endpointProvider);
            }).AddHostedService<SocketDataTransferringService>();
        }

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
    }
}