using System;
using System.Net.Sockets;

namespace HyperMsg.Transport.Sockets
{
    internal class AcceptedSocketConnection : IAcceptedConnection, IDisposable
    {
        private readonly Func<IBufferContext> bufferContextProvider;
        private readonly Socket socket;

        internal AcceptedSocketConnection(Func<IBufferContext> bufferContextProvider, Socket socket)
        {
            this.bufferContextProvider = bufferContextProvider;
            this.socket = socket;
        }

        internal bool ConnectionAcquired { get; set; }

        public IConnectionContext Acquire()
        {
            if (!socket.Connected)
            {
                throw new InvalidOperationException();
            }

            var bufferContext = bufferContextProvider.Invoke();
            bufferContext.TransmittingBuffer.FlushRequested += new SocketProxy(socket, null).TransmitAsync;
            ConnectionAcquired = true;
            return new SocketConnectionContext(bufferContext, socket);
        }

        public void Dispose()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}