using HyperMsg.Extensions;
using System;
using System.Net.Sockets;

namespace HyperMsg.Transport.Sockets
{
    internal class SocketDataObserver : IDisposable
    {
        private readonly IMessageSender messageSender;
        private readonly IBuffer receivingBuffer;

        private readonly Socket socket;
        private readonly SocketAsyncEventArgs socketEventArgs;

        private readonly object disposeSync = new object();
        private bool isDisposed = false;

        public SocketDataObserver(IMessagingContext messagingContext, IBufferContext bufferContext, Socket socket)
        {
            messageSender = messagingContext.Sender;
            receivingBuffer = bufferContext.ReceivingBuffer;

            this.socket = socket;
            socketEventArgs = new SocketAsyncEventArgs();
            socketEventArgs.Completed += DataReceivingCompleted;
        }

        public void HandleTransportEvent(TransportEvent transportEvent)
        {
            if (transportEvent == TransportEvent.Opened)
            {
                ResetBuffer();
                while (!socket.ReceiveAsync(socketEventArgs))
                {
                    AdvanceAndFlushBuffer();
                    ResetBuffer();
                }
            }
        }

        private void DataReceivingCompleted(object sender, SocketAsyncEventArgs e)
        {
            AdvanceAndFlushBuffer();
            ResetBuffer();
        }

        private void AdvanceAndFlushBuffer()
        {
            if (socketEventArgs.BytesTransferred == 0)
            {
                return;
            }

            receivingBuffer.Writer.Advance(socketEventArgs.BytesTransferred);
            messageSender.BufferReceivedData(receivingBuffer);
        }

        private void ResetBuffer()
        {
            var memory = receivingBuffer.Writer.GetMemory();

            lock (disposeSync)
            {
                if (!isDisposed)
                {
                    socketEventArgs.SetBuffer(memory);
                }
            }
        }

        public void Dispose()
        {
            lock (disposeSync)
            {
                socketEventArgs.Dispose();
                isDisposed = true;
            }
        }
    }
}
