using HyperMsg.Transport;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.WebSockets
{
    public class WebSocketConnectionService : ConnectionService, IHostedService
    {
        private readonly ClientWebSocket webSocket;
        private readonly Uri uri;

        public WebSocketConnectionService(ClientWebSocket webSocket, Uri uri, IMessagingContext messagingContext) : base(messagingContext)
        {
            this.webSocket = webSocket;
            this.uri = uri;
        }

        internal ClientWebSocket WebSocket => webSocket;

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override Task OpenConnectionAsync(CancellationToken cancellationToken) => webSocket.ConnectAsync(uri, cancellationToken);

        protected override Task CloseConnectionAsync(CancellationToken cancellationToken) => webSocket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, cancellationToken);
    }
}
