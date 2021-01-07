using HyperMsg.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;

namespace HyperMsg.Transport.Sockets
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSocketPort(this IServiceCollection services)
        {
            var socket = SocketFactory.CreateTcpSocket();
            var socketTransciever = new SocketTransceivingProxy(socket);

            return services.AddSingleton<IPort, SocketPortAdapter>()
                .AddSingleton(socket)
                .AddSingleton<ITransmitter>(socketTransciever)
                .AddSingleton<IReceiver>(socketTransciever)
                .AddObserver<SocketDataObserver, TransportEvent>(observar => observar.HandleTransportEvent);
        }

        public static IServiceCollection AddSocketTransport(this IServiceCollection services, string hostName, int port)
        {
            return services.AddSocketPort()
                        .AddTransportCommandObserver()
                        .AddBufferDataTransmitObserver()
                        .AddIPEndpoint(hostName, port);
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

        public static IServiceCollection AddConnectionObserver(this IServiceCollection services, Action<IAcceptedConnection> observer)
        {
            if (services.Any(s => s.ImplementationInstance is Action<IAcceptedConnection>))
            {
                var observers = services.Single(s => s.ImplementationInstance is Action<IAcceptedConnection>).ImplementationInstance as Action<IAcceptedConnection>;
                observers += observer;
                return services;
            }
            services.AddHostedService<SocketConnectionObservable>();
            return services.AddSingleton(observer);
        }

        public static IServiceCollection AddConnectionObserver(this IServiceCollection services, AsyncAction<IAcceptedConnection> observer)
        {
            if (services.Any(s => s.ImplementationInstance is Action<IAcceptedConnection>))
            {
                var observers = services.Single(s => s.ImplementationInstance is AsyncAction<IAcceptedConnection>).ImplementationInstance as AsyncAction<IAcceptedConnection>;
                observers += observer;                
                return services;
            }
            services.AddHostedService<SocketConnectionObservable>();
            return services.AddSingleton(observer);
        }
    }
}