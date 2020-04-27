using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport.Sockets
{
    internal sealed class SocketPortAdapter : IPort, IDisposable
    {
        private readonly Socket socket;
        private readonly EndPoint endPoint;
        private Stream stream;

        public SocketPortAdapter(Socket socket, EndPoint endPoint)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            this.endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }

        public bool ValidateAllCertificates { get; }

        public Stream Stream => GetStream();

        public bool IsOpen => socket.Connected;

        public void Open() => socket.Connect(endPoint);

        public Task OpenAsync(CancellationToken cancellationToken)
        {
            socket.Connect(endPoint);
            return Task.CompletedTask;
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            Close();
            return Task.CompletedTask;
        }

        public void Dispose() => Close();

        public void SetTls()
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
            RemoteCertificateValidationRequired?.Invoke(this, eventArgs);

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

        public event EventHandler<RemoteCertificateValidationEventArgs> RemoteCertificateValidationRequired;
    }
}