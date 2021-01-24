using Microsoft.Extensions.Hosting;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport.Sockets
{
    internal class SocketDataReceiver : MessagingObject, IHostedService
    {        
        private readonly IBuffer receivingBuffer;

        private readonly Socket socket;
        private readonly SocketAsyncEventArgs socketEventArgs;

        private readonly object disposeSync = new object();
        private bool isDisposed = false;

        public SocketDataReceiver(IMessagingContext messagingContext, IBufferContext bufferContext, Socket socket) : base(messagingContext)
        {            
            receivingBuffer = bufferContext.ReceivingBuffer;

            this.socket = socket;
            socketEventArgs = new SocketAsyncEventArgs();
            socketEventArgs.Completed += DataReceivingCompleted;
            AddHandler<TransportEvent>(HandleTransportEvent);
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
            Received(receivingBuffer);
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

        public override void Dispose()
        {
            lock (disposeSync)
            {
                socketEventArgs.Dispose();
                isDisposed = true;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
