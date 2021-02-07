using Microsoft.Extensions.Hosting;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Sockets
{
    internal class SocketDataTransferringService : MessagingObject, IHostedService
    {        
        private readonly IBuffer receivingBuffer;

        private readonly Socket socket;
        private readonly SocketAsyncEventArgs socketEventArgs;

        private readonly object disposeSync = new object();
        private bool isDisposed = false;

        public SocketDataTransferringService(IMessagingContext messagingContext, IBufferContext bufferContext, SocketConnectionService socketService) : base(messagingContext)
        {            
            receivingBuffer = bufferContext.ReceivingBuffer;

            socket = socketService.Socket;
            socketService.Connected = OnSocketConnected;
            socketEventArgs = new SocketAsyncEventArgs();
            socketEventArgs.Completed += DataReceivingCompleted;
            RegisterTransmitHandler<ReadOnlyMemory<byte>>(TransmitDataAsync);
        }

        private async Task TransmitDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            await socket.SendAsync(data, SocketFlags.None, cancellationToken);
        }

        private void OnSocketConnected()
        {
            ResetBuffer();
            while (!socket.ReceiveAsync(socketEventArgs))
            {
                AdvanceAndFlushBuffer();
                ResetBuffer();
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
            Receive(receivingBuffer);
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
