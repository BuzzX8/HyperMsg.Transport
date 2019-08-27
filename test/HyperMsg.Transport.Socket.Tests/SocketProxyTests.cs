using System;
using System.Net;
using System.Net.Sockets;

namespace HyperMsg.Transport.Socket
{
    public class SocketProxyTests : IDisposable
    {
        private const int port = 8080;
        private const int bufferSize = 1024;
        private readonly EndPoint endPoint;

        private readonly SocketProxy socketProxy;

        private readonly System.Net.Sockets.Socket listeningSocket;
        private readonly byte[] receiveBuffer;
        private System.Net.Sockets.Socket AcceptedSocket;

        public SocketProxyTests()
        {
            endPoint = new IPEndPoint(IPAddress.Loopback, port);

            socketProxy = new SocketProxy(new System.Net.Sockets.Socket(SocketType.Stream, ProtocolType.Tcp), endPoint);

            listeningSocket = new System.Net.Sockets.Socket(SocketType.Stream, ProtocolType.Tcp);
            listeningSocket.Bind(endPoint);
            listeningSocket.Listen(1);

            receiveBuffer = new byte[bufferSize];
        }

        protected ReadOnlySpan<byte> GetReceivedBytes()
        {
            var received = AcceptedSocket.Receive(receiveBuffer);
            return new ReadOnlySpan<byte>(receiveBuffer, 0, received);
        }

        public void Dispose()
        {
            listeningSocket.Close();
            AcceptedSocket?.Close();
        }
    }
}
