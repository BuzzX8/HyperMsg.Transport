using System;
using System.Net;
using System.Net.Sockets;

namespace HyperMsg.Socket
{
    public static class ConfigurableExtensions
    {
        public static void UseSockets(this IConfigurable configurable, EndPoint endpoint)
        {
            configurable.AddSetting(nameof(EndPoint), endpoint);
            configurable.RegisterConfigurator((p, s) =>
            {
                var endPoint = (EndPoint)s[nameof(EndPoint)];
                var socket = CreateDefaultSocket(endPoint);
                RegisterHandlers(p, socket);
            });
        }

        private static void RegisterHandlers(IServiceProvider serviceProvider, ISocket socket)
        {
            var receivingBuffer = (IReceivingBuffer)serviceProvider.GetService(typeof(IReceivingBuffer));
            var transmittingBuffer = (ITransmittingBuffer)serviceProvider.GetService(typeof(ITransmittingBuffer));
            var messageSender = (IMessageSender)serviceProvider.GetService(typeof(IMessageSender));
            var handlerRegistry = (IMessageHandlerRegistry)serviceProvider.GetService(typeof(IMessageHandlerRegistry));

            var commandHandler = new SocketCommandHandler(socket, messageSender);
            var dataHandler = new SocketDataHandler(socket, receivingBuffer);
            var worker = new TransportWorker(dataHandler.FetchSocketDataAsync);

            handlerRegistry.Register<TransportCommand>(commandHandler.HandleCommandAsync);
            transmittingBuffer.FlushRequested += dataHandler.HandleBufferFlushAsync;
            handlerRegistry.Register<TransportEvent>(worker.HandleTransportEventAsync);
        }

        private static ISocket CreateDefaultSocket(EndPoint endPoint)
        {            
            var socket = new System.Net.Sockets.Socket(SocketType.Stream, ProtocolType.Tcp);

            return new SocketProxy(socket, endPoint);
        }
    }
}
