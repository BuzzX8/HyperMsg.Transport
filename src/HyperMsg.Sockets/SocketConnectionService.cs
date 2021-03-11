using HyperMsg.Transport;
using HyperMsg.Transport.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Sockets
{
    internal class SocketConnectionService : ConnectionService
    {
        private readonly Socket socket;
        private Lazy<EndPoint> endPoint;

        public SocketConnectionService(IMessagingContext messagingContext, Socket socket, Func<EndPoint> endpointProvider) : base(messagingContext)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            endPoint = new Lazy<EndPoint>(endpointProvider) ?? throw new ArgumentNullException(nameof(endPoint));
        }

        protected override IEnumerable<IDisposable> GetDefaultDisposables()
        {
            return base.GetDefaultDisposables()
                .Concat(new[] { this.RegisterSetEndpointHandler<EndPoint>(SetEndpoint), this.RegisterSetEndpointHandler<IPEndPoint>(SetEndpoint)});
        }

        private bool IsOpen => socket.Connected;

        protected override Task OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (IsOpen)
            {
                return Task.CompletedTask;
            }

            try
            {
                return Task.Run(() => socket.Connect(endPoint.Value), cancellationToken);
            }
            catch(SocketException e)
            {
                throw new TransportException(e.Message, e);
            }
        }

        protected override Task CloseConnectionAsync(CancellationToken cancellationToken)
        {
            if (!IsOpen)
            {
                return Task.CompletedTask;
            }                        
            
            socket.Shutdown(SocketShutdown.Both);            
            socket.Disconnect(true);

            return Task.CompletedTask;
        }

        private void SetEndpoint(EndPoint endPoint) => this.endPoint = new Lazy<EndPoint>(() => endPoint);

        private void SetEndpoint(IPEndPoint endPoint) => this.endPoint = new Lazy<EndPoint>(() => endPoint);

        public override void Dispose()
        {
            base.Dispose();            
            socket.Dispose();
        }
    }
}