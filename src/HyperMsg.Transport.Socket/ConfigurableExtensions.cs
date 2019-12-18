using HyperMsg.Transport;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Socket
{
    public static class ConfigurableExtensions
    {
        private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(200);

        public static void UseSockets(this IConfigurable configurable, EndPoint endpoint, ReceiveMode receiveMode = ReceiveMode.Polling)
        {
            var socket = CreateDefaultSocket(endpoint);
            configurable.UseConnection(socket);
            configurable.UseTransmitter(socket);

            switch(receiveMode)
            {
                case ReceiveMode.Polling:
                    configurable.UsePollDataHandler(socket, DefaultPollInterval);
                    break;

                case ReceiveMode.Reactive:
                    configurable.UseSocketObserver(socket.InnerSocket);
                    break;

                case ReceiveMode.Worker:
                    RegisterSocketWorker(configurable, socket);
                    break;
            }
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

        private static void RegisterSocketWorker(IConfigurable configurable, SocketProxy socket)
        {
            configurable.RegisterConfigurator((p, s) =>
            {
                var buffer = (IReceivingBuffer)p.GetService(typeof(IReceivingBuffer));
                var workItem = new SocketWorkItem(buffer, socket);
                configurable.UseWorkerDataHandler(workItem.FetchSocketDataAsync);
            });
        }

        private static SocketProxy CreateDefaultSocket(EndPoint endPoint)
        {            
            var socket = new System.Net.Sockets.Socket(SocketType.Stream, ProtocolType.Tcp);

            return new SocketProxy(socket, endPoint);
        }
    }

    internal class SocketWorkItem
    {
        IBuffer buffer;
        IReceiver receiver;

        internal SocketWorkItem(IBuffer buffer, IReceiver receiver)
        {
            this.buffer = buffer;
            this.receiver = receiver;
        }

        internal async Task FetchSocketDataAsync(CancellationToken cancellationToken)
        {
            var memory = buffer.Writer.GetMemory();
            await receiver.ReceiveAsync(memory, cancellationToken);
        }
    }
}
