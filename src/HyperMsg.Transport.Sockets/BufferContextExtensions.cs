using System.Net;

namespace HyperMsg.Transport.Sockets
{
    public static class BufferContextExtensions
    {
        public static IPort AttachClientSocket(this IBufferContext bufferContext, EndPoint endPoint)
        {
            var socket = SocketFactory.CreateTcpSocket();
            var observer = new SocketDataObserver(bufferContext.ReceivingBuffer, socket);
            var socketProxy = new SocketProxy(socket, endPoint);
            socketProxy.Connected += observer.Run;
            bufferContext.TransmittingBuffer.FlushRequested += socketProxy.TransmitAsync;

            return socketProxy;
        }
    }
}
