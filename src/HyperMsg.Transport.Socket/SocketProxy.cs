using HyperMsg.Transport;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Socket
{
    public class SocketProxy : IConnection, ITransmitter, IReceiver, IDisposable
    {
        private readonly System.Net.Sockets.Socket socket;        
        private readonly EndPoint endpoint;
        private Stream stream;

        public SocketProxy(System.Net.Sockets.Socket socket, EndPoint endpoint)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            this.endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public System.Net.Sockets.Socket InnerSocket => socket;

        public bool ValidateAllCertificates { get; }

        public Stream Stream => GetStream();

        public bool IsConnected => socket.Connected;

        #region IConnection

        public void Connect() => socket.Connect(endpoint);

        public Task ConnectAsync(CancellationToken cancellationToken) => socket.ConnectAsync(endpoint);

        public void Disconnect() => socket.Disconnect(true);

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            Disconnect();
            return Task.CompletedTask;
        }

        #endregion

        #region ITransmitter

        public void Transmit(ReadOnlyMemory<byte> data) => socket.Send(data.Span);

        public Task TransmitAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) => socket.SendAsync(data, SocketFlags.None, cancellationToken).AsTask();

        #endregion

        #region IReceiver

        public int Receive(Memory<byte> buffer) => socket.Receive(buffer.Span);

        public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) => socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).AsTask();

        #endregion

        public void Dispose() => socket.Dispose();

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
            sslStream.AuthenticateAsClient(endpoint.ToString());
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
