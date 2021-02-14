using HyperMsg.Connection;
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
    internal class SocketConnectionService : MessagingObject
    {
        private readonly Socket socket;
        private readonly Lazy<EndPoint> endPoint;
        private Stream stream;

        public SocketConnectionService(IMessagingContext messagingContext, Socket socket, Func<EndPoint> endpointProvider) : base(messagingContext)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            endPoint = new Lazy<EndPoint>(endpointProvider) ?? throw new ArgumentNullException(nameof(endPoint));

            RegisterHandler(ConnectionCommand.Open, OpenAsync);
            RegisterHandler(ConnectionCommand.Close, CloseAsync);
            RegisterHandler(ConnectionCommand.SetTransportLevelSecurity, SetTls);
        }

        public bool ValidateAllCertificates { get; }

        public Socket Socket => socket;

        public Stream Stream => GetStream();

        private bool IsOpen => socket.Connected;

        private async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (IsOpen)
            {
                return;
            }

            await SendAsync(ConnectionEvent.Opening, cancellationToken);
            socket.Connect(endPoint.Value);
            Connected?.Invoke();
            await SendAsync(ConnectionEvent.Opened, cancellationToken);            
        }

        private async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (!IsOpen)
            {
                return;
            }

            Closing?.Invoke();
            await SendAsync(ConnectionEvent.Closing, cancellationToken);
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            await SendAsync(ConnectionEvent.Closed, cancellationToken);

            return;
        }        

        private void SetTls()
        {
            if (stream == null)
            {
                throw new InvalidOperationException();
            }

            if (stream is SslStream)
            {
                return;
            }

            var sslStream = new SslStream(stream, false, ValidateRemoteCertificate);
            sslStream.AuthenticateAsClient(endPoint.ToString());
            stream = sslStream;
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

        public Action Connected;

        public Action Closing;

        public Action<RemoteCertificateValidationEventArgs> RemoteCertificateValidationRequired;
    }
}