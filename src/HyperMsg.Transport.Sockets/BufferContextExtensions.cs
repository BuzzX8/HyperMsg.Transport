using System.Net;

namespace HyperMsg.Transport.Sockets
{
    public static class BufferContextExtensions
    {
        public static IPort AttachClientSocket(this IBufferContext bufferContext, EndPoint endPoint)
        {
            var socket = SocketFactory.CreateTcpSocket();
            var socketProxy = new SocketProxy(socket, endPoint);
            bufferContext.AttachSocket(socketProxy);

            return socketProxy;
        }

        internal static void AttachSocket(this IBufferContext bufferContext, SocketProxy socket)
        {
            var observer = new SocketDataObserver(bufferContext.ReceivingBuffer, socket.InnerSocket);
            socket.Connected += observer.Run;
            bufferContext.TransmittingBuffer.FlushRequested += socket.TransmitAsync;
        }
    }
}
