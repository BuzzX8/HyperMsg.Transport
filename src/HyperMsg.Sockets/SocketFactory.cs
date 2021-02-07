using System.Net.Sockets;

namespace HyperMsg.Sockets
{
    internal static class SocketFactory
    {
        internal static Socket CreateTcpSocket() => new Socket(SocketType.Stream, ProtocolType.Tcp);
    }
}
