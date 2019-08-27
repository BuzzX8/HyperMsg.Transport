using System.Net;
using System.Net.Sockets;

namespace HyperMsg.Transport.Socket
{
    public static class ConfigurableExtensions
    {
        public static void UseSockets(this IConfigurable configurable, EndPoint endpoint)
        {
            configurable.AddSetting(nameof(EndPoint), endpoint);
            configurable.RegisterService(typeof(ITransport), (p, s) =>
            {
                var ep = (EndPoint)s[nameof(EndPoint)];
                var socket = new System.Net.Sockets.Socket(SocketType.Stream, ProtocolType.Tcp);
                var proxy = new SocketProxy(socket, ep);

                var receivingBuffer = (IReceivingBuffer)p.GetService(typeof(IReceivingBuffer));
                var sendingBuffer = (ISendingBuffer)p.GetService(typeof(ISendingBuffer));
                var transport = new SocketTransport(proxy, receivingBuffer.Writer, receivingBuffer.FlushAsync);
                sendingBuffer.FlushRequested += transport.HandleFlushRequestAsync;

                return transport;
            });
        }
    }
}
