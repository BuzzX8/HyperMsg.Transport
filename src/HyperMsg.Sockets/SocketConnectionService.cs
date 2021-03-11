using HyperMsg.Transport;
using HyperMsg.Transport.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Sockets
{
    internal class SocketConnectionService : ConnectionService
    {
        private readonly Socket socket;
        private Lazy<EndPoint> endPoint;
        private Stream stream;

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

        public bool ValidateAllCertificates { get; }

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

        protected override Task SetTransportLevelSecurityAsync(CancellationToken _)
        {
            if (stream == null)
            {
                throw new InvalidOperationException();
            }

            if (stream is SslStream)
            {
                return Task.CompletedTask;
            }

            var sslStream = new SslStream(stream, false, ValidateRemoteCertificate);
            sslStream.AuthenticateAsClient(endPoint.ToString());
            stream = sslStream;

            return Task.CompletedTask;
        }

        private bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (ValidateAllCertificates)
            {
                return true;
            }

            var eventArgs = new RemoteCertificateValidationEventArgs(certificate, chain, sslPolicyErrors);
            RemoteCertificateValidationRequired?.Invoke(eventArgs);

            return eventArgs.IsValid;
        }

        private Stream GetStream()
        {
            if (stream == null)
            {
                stream = new NetworkStream(socket);
            }

            return stream;
        }

        private void SetEndpoint(EndPoint endPoint)
        {
            this.endPoint = new Lazy<EndPoint>(() => endPoint);
        }

        private void SetEndpoint(IPEndPoint endPoint) => this.endPoint = new Lazy<EndPoint>(() => endPoint);

        public override void Dispose()
        {
            base.Dispose();            
            socket.Dispose();
        }

        public Action<RemoteCertificateValidationEventArgs> RemoteCertificateValidationRequired;
    }
}