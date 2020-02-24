using System;
using System.Buffers;
using System.Net;

namespace HyperMsg.Transport.Sockets
{
    public abstract class SocketFixtureBase : IDisposable
    {
        private const int DefaultBufferSize = 2048;
        private readonly IPEndPoint EndPoint;

        protected IBufferContext ClientContext;
        protected IBufferContext ListenerContext;

        protected readonly IPort ClientConnection;
        protected readonly SocketConnectionListener ConnectionListener;

        public SocketFixtureBase(int port)
        {
            EndPoint = new IPEndPoint(IPAddress.Loopback, port);
            var bufferFactory = new BufferFactory(MemoryPool<byte>.Shared);
            ClientContext = bufferFactory.CreateContext(DefaultBufferSize, DefaultBufferSize);
            ListenerContext = bufferFactory.CreateContext(DefaultBufferSize, DefaultBufferSize);
            ClientConnection = ClientContext.AttachClientSocket(EndPoint);
            ConnectionListener = new SocketConnectionListener(() => bufferFactory.CreateContext(DefaultBufferSize, DefaultBufferSize), EndPoint);
        }

        public void Dispose()
        {
            ClientConnection.Close();
            ConnectionListener.Close();
        }
    }
}
