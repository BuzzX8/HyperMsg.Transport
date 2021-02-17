using HyperMsg.Connection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.WebSockets
{
    public class WebSocketConnectionService : MessagingObject, IHostedService
    {
        private readonly ClientWebSocket webSocket;
        private readonly Uri uri;

        public WebSocketConnectionService(ClientWebSocket webSocket, Uri uri, IMessagingContext messagingContext) : base(messagingContext)
        {
            this.webSocket = webSocket;
            this.uri = uri;
            RegisterHandler(ConnectionCommand.Open, OpenAsync);
            RegisterHandler(ConnectionCommand.Close, CloseAsync);
        }

        internal ClientWebSocket WebSocket => webSocket;

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task OpenAsync(CancellationToken cancellationToken)
        {
            await webSocket.ConnectAsync(uri, cancellationToken);
            await SendAsync(ConnectionEvent.Opened, cancellationToken);
        }

        private Task CloseAsync(CancellationToken cancellationToken)
        {
            return webSocket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, cancellationToken);
        }
    }
}
