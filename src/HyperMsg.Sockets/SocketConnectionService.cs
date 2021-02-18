using HyperMsg.Transport;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Sockets
{
    internal class SocketConnectionService : ConnectionService, IHostedService
    {
        private readonly Socket socket;
        private readonly Lazy<EndPoint> endPoint;
        private Stream stream;

        public SocketConnectionService(IMessagingContext messagingContext, Socket socket, Func<EndPoint> endpointProvider) : base(messagingContext)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            endPoint = new Lazy<EndPoint>(endpointProvider) ?? throw new ArgumentNullException(nameof(endPoint));
        }

        public bool ValidateAllCertificates { get; }

        public Socket Socket => socket;

        public Stream Stream => GetStream();

        private bool IsOpen => socket.Connected;

        protected override Task OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (IsOpen)
            {
                return Task.CompletedTask;
            }
                        
            socket.Connect(endPoint.Value);
            return Task.CompletedTask;
        }

        protected override Task CloseConnectionAsync(CancellationToken cancellationToken)
        {
            if (!IsOpen)
            {
                return Task.CompletedTask;
            }                        
            
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

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

        public override void Dispose()
        {
            base.Dispose();            
            socket.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Action<RemoteCertificateValidationEventArgs> RemoteCertificateValidationRequired;
    }
}