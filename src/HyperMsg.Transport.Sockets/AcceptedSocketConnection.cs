using HyperMsg.Transport.Sockets.Properties;
using System;
using System.Net.Sockets;

namespace HyperMsg.Transport.Sockets
{
    internal class AcceptedSocketConnection : IAcceptedConnection, IDisposable
    {
        private readonly Socket socket;

        internal AcceptedSocketConnection(Socket socket)
        {
            this.socket = socket;
        }

        internal bool ConnectionAcquired { get; set; }

        public IConnectionContext Acquire()
        {
            if (!socket.Connected)
            {
                throw new InvalidOperationException(Resources.AcceptedSocketConnection_SocketClosedError);
            }
            
            ConnectionAcquired = true;
            return new SocketConnectionContext(socket);
        }

        public void Dispose()
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            socket.Close();
        }
    }
}