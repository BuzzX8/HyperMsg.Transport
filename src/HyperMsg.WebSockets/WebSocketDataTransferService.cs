using HyperMsg.Transport;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.WebSockets
{
    public class WebSocketDataTransferService : DataTransferService, IHostedService
    {
        private readonly ClientWebSocket webSocket;
        private readonly IBuffer receivingBuffer;

        private ValueTask<ValueWebSocketReceiveResult> currentReceiveTask;

        public WebSocketDataTransferService(ClientWebSocket webSocket, IBuffer receivingBuffer, IMessagingContext messagingContext) : base(messagingContext)
        {
            this.webSocket = webSocket;
            this.receivingBuffer = receivingBuffer;
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override async Task TransmitDataAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) => await webSocket.SendAsync(buffer, WebSocketMessageType.Binary, false, cancellationToken);

        protected override Task OnConnectionOpenedAsync(CancellationToken cancellationToken)
        {
            ReceiveAsync();
            return Task.CompletedTask;
        }

        private void ReceiveAsync()
        {
            var memory = receivingBuffer.Writer.GetMemory();
            currentReceiveTask = webSocket.ReceiveAsync(memory, default);
            currentReceiveTask.GetAwaiter().OnCompleted(() =>
            {
                var result = currentReceiveTask.Result;
                receivingBuffer.Writer.Advance(result.Count);
                Receive(receivingBuffer);
                ReceiveAsync();
            });
        }
    }
}
