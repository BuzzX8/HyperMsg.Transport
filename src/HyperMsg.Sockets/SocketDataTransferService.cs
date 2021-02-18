using HyperMsg.Transport;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Sockets
{
    internal class SocketDataTransferService : DataTransferService, IHostedService
    {
        private readonly IBuffer receivingBuffer;
        private readonly Socket socket;

        private CancellationTokenSource tokenSource = new();
        private ValueTask<int> currentReceiveTask;

        public SocketDataTransferService(Socket socket, IBuffer receivingBuffer, IMessagingContext messagingContext) : base(messagingContext)
        {            
            this.receivingBuffer = receivingBuffer;
            this.socket = socket;
        }

        protected override async Task TransmitDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) => await socket.SendAsync(data, SocketFlags.None, cancellationToken);

        protected override Task OnConnectionOpenedAsync(CancellationToken _)
        {
            tokenSource = new();
            ReceiveData();
            return Task.CompletedTask;
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

        protected override Task OnConnectionClosingAsync(CancellationToken _)
        {
            tokenSource.Cancel();
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            base.Dispose();
            tokenSource.Dispose();
        }

        public Task StartAsync(CancellationToken _) => Task.CompletedTask;

        public Task StopAsync(CancellationToken _) => Task.CompletedTask;
    }
}
