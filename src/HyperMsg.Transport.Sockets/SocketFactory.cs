using System.Net.Sockets;

namespace HyperMsg.Transport.Sockets
{
    internal static class SocketFactory
    {
        internal static Socket CreateTcpSocket() => new Socket(SocketType.Stream, ProtocolType.Tcp);
    }
}
