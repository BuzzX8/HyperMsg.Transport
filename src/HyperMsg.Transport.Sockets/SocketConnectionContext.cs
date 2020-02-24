using System;
using System.Net.Sockets;

namespace HyperMsg.Transport.Sockets
{
    internal class SocketConnectionContext : IConnectionContext
    {
        private readonly Socket socket;
        private readonly SocketDataObserver socketObserver;

        internal SocketConnectionContext(IBufferContext bufferContext, Socket socket)
        {
            BufferContext = bufferContext;
            this.socket = socket;
            socketObserver = new SocketDataObserver(bufferContext.ReceivingBuffer, socket);
            socketObserver.Run();
        }

        public IBufferContext BufferContext { get; }

        public void Dispose()
        {            
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

            if (BufferContext is IDisposable disp)
            {
                disp.Dispose();
            }
        }
    }
}
