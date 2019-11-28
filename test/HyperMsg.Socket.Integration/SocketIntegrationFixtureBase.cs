using HyperMsg.Integration;
using System;
using System.Net;
using System.Net.Sockets;

namespace HyperMsg.Socket
{
    public class SocketIntegrationFixtureBase : IntegrationFixtureBase, IDisposable
    {
        private const int DefaultBufferSize = 2048;

        private readonly IPEndPoint endPoint;
        private readonly System.Net.Sockets.Socket listeningSocket;        

        public SocketIntegrationFixtureBase() : base(DefaultBufferSize, DefaultBufferSize)
        {
            endPoint = new IPEndPoint(IPAddress.Loopback, 8080);
            listeningSocket = new System.Net.Sockets.Socket(SocketType.Stream, ProtocolType.Tcp);
            Configurable.UseSockets(endPoint);
            listeningSocket.Bind(endPoint);
        }

        protected System.Net.Sockets.Socket AcceptedSocket { get; private set; }

        protected void AcceptSocket()
        {
            var task = listeningSocket.AcceptAsync();
            if (task.Wait(TimeSpan.FromSeconds(5)))
            {
                AcceptedSocket = task.Result;
            }
        }

        protected void StartListening() => listeningSocket.Listen(1);

        public void Dispose()
        {
            AcceptedSocket?.Dispose();
            listeningSocket?.Dispose();
        }
    }
}
