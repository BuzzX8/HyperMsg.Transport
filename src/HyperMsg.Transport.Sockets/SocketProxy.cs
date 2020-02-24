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
    internal sealed class SocketProxy : IPort, ITransmitter, IReceiver, IDisposable
    {
        private readonly Socket socket;
        private readonly EndPoint endpoint;
        private Stream stream;

        public SocketProxy(Socket socket, EndPoint endpoint)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            this.endpoint = endpoint;
        }

        public Socket InnerSocket => socket;

        public bool ValidateAllCertificates { get; }

        public Stream Stream => GetStream();

        public bool IsOpen => socket.Connected;

        #region IPort

        public void Open()
        {
            socket.Connect(endpoint);
            Connected?.Invoke();
        }

        public async Task OpenAsync(CancellationToken cancellationToken)
        {
            await socket.ConnectAsync(endpoint);
            Connected?.Invoke();
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

        #endregion

        internal async Task TransmitAsync(IBufferReader<byte> reader, CancellationToken cancellationToken)
        {
            ThrowIfDisconnected();
            var data = reader.Read();

            if (data.Length == 0)
            {
                return;
            }

            if (data.IsSingleSegment)
            {
                await TransmitAsync(data.First, cancellationToken);
                reader.Advance((int)data.Length);
                return;
            }

            var enumerator = data.GetEnumerator();

            while(enumerator.MoveNext())
            {
                await TransmitAsync(enumerator.Current, cancellationToken);
                reader.Advance(enumerator.Current.Length);
            }
        }

        #region ITransmitter

        public void Transmit(ReadOnlyMemory<byte> data) => socket.Send(data.Span);

        public Task TransmitAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) => socket.SendAsync(data, SocketFlags.None, cancellationToken).AsTask();

        #endregion

        #region IReceiver

        public int Receive(Memory<byte> buffer) => socket.Receive(buffer.Span);

        public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) => socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).AsTask();

        #endregion

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

        private void ThrowIfDisconnected()
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException();
            }
        }

        internal event Action Connected;

        public event EventHandler<RemoteCertificateValidationEventArgs> RemoteCertificateValidationRequired;
    }
}
