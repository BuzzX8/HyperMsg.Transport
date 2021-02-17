using HyperMsg.Connection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.WebSockets
{
    public class WebSocketDataTransferService : MessagingObject, IHostedService
    {
        private readonly ClientWebSocket webSocket;
        private readonly IBuffer receivingBuffer;

        ValueTask<ValueWebSocketReceiveResult> currentReceiveTask;

        public WebSocketDataTransferService(IBufferContext bufferContext, WebSocketConnectionService connectionService, IMessagingContext messagingContext) : base(messagingContext)
        {
            webSocket = connectionService.WebSocket;
            receivingBuffer = bufferContext.ReceivingBuffer;
            RegisterHandler(ConnectionEvent.Opened, OnConnected);
            RegisterTransmitHandler<ReadOnlyMemory<byte>>(TransmitDataAsync);
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task TransmitDataAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            await webSocket.SendAsync(buffer, WebSocketMessageType.Binary, false, cancellationToken);
        }

        private void OnConnected()
        {
            ReceiveAsync();
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
