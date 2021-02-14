using Microsoft.Extensions.Hosting;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Sockets
{
    internal class SocketDataTransferringService : MessagingObject, IHostedService
    {
        private CancellationTokenSource tokenSource = new();
        private readonly IBuffer receivingBuffer;
        private readonly Socket socket;
        private ValueTask<int> currentReceiveTask;

        public SocketDataTransferringService(IMessagingContext messagingContext, IBufferContext bufferContext, SocketConnectionService socketService) : base(messagingContext)
        {            
            receivingBuffer = bufferContext.ReceivingBuffer;
            socket = socketService.Socket;
            socketService.Connected = OnConnected;
            socketService.Closing = OnSocketClosing;
            RegisterTransmitHandler<ReadOnlyMemory<byte>>(TransmitDataAsync);
        }

        private async Task TransmitDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) => await socket.SendAsync(data, SocketFlags.None, cancellationToken);

        private void OnConnected()
        {
            tokenSource = new();
            ReceiveData();
        }

        private void ReceiveData()
        {            
            var memory = receivingBuffer.Writer.GetMemory();
            currentReceiveTask = socket.ReceiveAsync(memory, SocketFlags.None, tokenSource.Token);
            currentReceiveTask.GetAwaiter().OnCompleted(() =>
            {
                if (tokenSource.IsCancellationRequested)
                {
                    return;
                }

                var bytesReceived = currentReceiveTask.Result;
                receivingBuffer.Writer.Advance(bytesReceived);
                Receive(receivingBuffer);
                ReceiveData();
            });
        }

        private void OnSocketClosing() => tokenSource.Cancel();

        public override void Dispose()
        {
            base.Dispose();
            tokenSource.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
