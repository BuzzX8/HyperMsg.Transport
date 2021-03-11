using HyperMsg.Transport;
using HyperMsg.Transport.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.WebSockets
{
    internal class WebSocketConnectionService : ConnectionService, IHostedService
    {
        private readonly ClientWebSocket webSocket;
        private Uri uri;

        public WebSocketConnectionService(ClientWebSocket webSocket, Uri uri, IMessagingContext messagingContext) : base(messagingContext)
        {
            this.webSocket = webSocket;
            this.uri = uri;
            
        }

        internal ClientWebSocket WebSocket => webSocket;

        protected override IEnumerable<IDisposable> GetDefaultDisposables()
        {
            yield return this.RegisterSetEndpointHandler<Uri>(SetEndpoint);
        }

        protected override Task OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (uri == null)
            {
                throw new TransportException("Endpoint does not provided.");
            }
            return webSocket.ConnectAsync(uri, cancellationToken);
        }

        protected override Task CloseConnectionAsync(CancellationToken cancellationToken) => webSocket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, cancellationToken);

        private void SetEndpoint(Uri uri) => this.uri = uri;
    }
}
