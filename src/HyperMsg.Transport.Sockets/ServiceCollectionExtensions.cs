using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Sockets;

namespace HyperMsg.Transport.Sockets
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSocketServices(this IServiceCollection services)
        {
            var socket = SocketFactory.CreateTcpSocket();

            return services.AddSingleton(socket)
                .AddConnectionCommandService<SocketConnectionService>()
                .AddBufferTransmitter((buffer, token) => socket.SendAsync(buffer, SocketFlags.None, token).AsTask())
                .AddHostedService<SocketDataReceiver>();
        }

        public static IServiceCollection AddIPEndpoint(this IServiceCollection services, string hostName, int port)
        {
            return services.AddSingleton<EndPoint>(provider =>
            {
                var addresses = Dns.GetHostAddresses(hostName);
                return new IPEndPoint(addresses[0], port);
            });
        }

        public static IServiceCollection AddIPEndPoint(this IServiceCollection services, IPAddress address, int port) => services.AddSingleton<EndPoint>(new IPEndPoint(address, port));

        public static IServiceCollection AddLocalIPEndPoint(this IServiceCollection services, int port) => services.AddIPEndPoint(IPAddress.Loopback, port);
    }
}