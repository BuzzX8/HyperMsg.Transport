using HyperMsg.Transport;
using System;
using System.Net;
using System.Net.Sockets;

namespace HyperMsg.Socket
{
    public static class ConfigurableExtensions
    {
        private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(200);

        public static void UseSockets(this IConfigurable configurable, EndPoint endpoint, bool usePoll = false)
        {
            var socket = CreateDefaultSocket(endpoint);
            configurable.UseConnection(socket);
            configurable.UseTransmitter(socket);

            if (usePoll)
            {
                configurable.UsePollDataHandler(socket, DefaultPollInterval);
                return;
            }

            configurable.UseSocketObserver(socket.InnerSocket);
        }

        private static void UseSocketObserver(this IConfigurable configurable, System.Net.Sockets.Socket socket)
        {
            configurable.AddSetting(nameof(socket), socket);
            configurable.RegisterConfigurator((p, s) =>
            {
                var receivingBuffer = (IReceivingBuffer)p.GetService(typeof(IReceivingBuffer));
                var handlerRegistry = (IMessageHandlerRegistry)p.GetService(typeof(IMessageHandlerRegistry));
                var observer = new SocketObserver(receivingBuffer, (System.Net.Sockets.Socket)s[nameof(socket)]);
                handlerRegistry.Register<TransportEvent>(observer.Handle);
            });
        }

        private static SocketProxy CreateDefaultSocket(EndPoint endPoint)
        {            
            var socket = new System.Net.Sockets.Socket(SocketType.Stream, ProtocolType.Tcp);

            return new SocketProxy(socket, endPoint);
        }
    }
}
