using System;
using System.Net.Sockets;

namespace HyperMsg.Transport.Sockets
{
    internal class SocketDataObserver : IDisposable
    {
        private readonly IMessageSender messageSender;
        private readonly IBuffer receivingBuffer;
        private readonly IDisposable subscription;

        private readonly Socket socket;
        private readonly SocketAsyncEventArgs socketEventArgs;        

        public SocketDataObserver(IMessagingContext messagingContext, IBuffer receivingBuffer, Socket socket)
        {
            messageSender = messagingContext.Sender;
            this.receivingBuffer = receivingBuffer ?? throw new ArgumentNullException(nameof(receivingBuffer));            
            subscription = messagingContext.Observable.Subscribe<TransportEvent>(HandleTransportEvent);

            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            socketEventArgs = new SocketAsyncEventArgs();
            socketEventArgs.Completed += DataReceivingCompleted;
        }

        private void HandleTransportEvent(TransportEvent transportEvent)
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
            socketEventArgs.SetBuffer(memory);
        }

        public void Dispose()
        {
            socketEventArgs.Dispose();
            subscription.Dispose();
        }
    }
}
