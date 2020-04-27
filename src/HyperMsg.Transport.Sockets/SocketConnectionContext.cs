using System.Net.Sockets;

namespace HyperMsg.Transport.Sockets
{
    internal class SocketConnectionContext : IConnectionContext
    {
        private readonly Socket socket;
        
        private readonly SocketTransceivingProxy transceivingProxy;

        internal SocketConnectionContext(Socket socket)
        {            
            this.socket = socket;
            transceivingProxy = new SocketTransceivingProxy(socket);
        }

        public ITransmitter Transmitter => transceivingProxy;

        public IReceiver Receiver => transceivingProxy;

        public void Dispose()
        {            
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}