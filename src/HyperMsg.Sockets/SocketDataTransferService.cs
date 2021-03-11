using HyperMsg.Extensions;
using HyperMsg.Transport;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Sockets
{
    internal class SocketDataTransferService : DataTransferService, IHostedService
    {
        private readonly IBuffer receivingBuffer;
        private readonly Socket socket;
        private Stream stream;

        private SocketAsyncEventArgs eventArgs;

        public SocketDataTransferService(Socket socket, IBuffer receivingBuffer, IMessagingContext messagingContext) : base(messagingContext)
        {            
            this.receivingBuffer = receivingBuffer;
            this.socket = socket;
            ValidateAllCertificates = true;
        }

        public bool ValidateAllCertificates { get; }

        private Stream Stream => stream ??= new NetworkStream(socket);

        protected override IEnumerable<IDisposable> GetDefaultDisposables()
        {
            return base.GetDefaultDisposables().Concat(new[] { this.RegisterHandler(ConnectionCommand.SetTransportLevelSecurity, SetTransportLevelSecurityAsync) });
        }

        protected override async Task TransmitDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            try
            {
                await socket.SendAsync(data, SocketFlags.None, cancellationToken);
            }
            catch(SocketException e)
            {                
                throw new TransportException(e.Message, e);
            }
        }

        protected override Task OnConnectionOpenedAsync(CancellationToken _)
        {
            eventArgs = new();            
            eventArgs.Completed += OnSocketReceiveCompleted;            

            ResetBuffer();
            Task.Run(ReceiveData).ContinueWith(OnBootstrapCompleted);
            return Task.CompletedTask;
        }

        private void ReceiveData()
        {            
            if (!socket.ReceiveAsync(eventArgs) && socket.Connected)
            {
                OnSocketReceiveCompleted(this, eventArgs);
            }            
        }

        private void OnBootstrapCompleted(Task receiveTask)
        {
            if (!receiveTask.IsCompletedSuccessfully && receiveTask.Exception != null)
            {
                receiveTask.Exception.Flatten();
                var exception = receiveTask.Exception.InnerException;
                Send(exception);
            }
        }

        private void OnSocketReceiveCompleted(object sender, SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs.SocketError != SocketError.Success && !socket.Connected)
            {
                return;
            }

            receivingBuffer.Clear();
            var memory = receivingBuffer.Writer.GetMemory();
            
            var bytesReceived = Stream.Read(memory.Span);

            if (bytesReceived > 0)
            {
                receivingBuffer.Writer.Advance(bytesReceived);
                this.ReceiveAsync(receivingBuffer, default).ContinueWith(t =>
                {
                    ResetBuffer();
                    ReceiveData();
                });

                return;
            }

            if (!socket.Connected)
            {
                socket.Disconnect(true);
                Send(ConnectionEvent.Closed);
                return;
            }

            Debugger.Launch();
        }

        private void ResetBuffer()
        {
            eventArgs.SetBuffer(Array.Empty<byte>(), 0, 0);
        }

        protected override Task OnConnectionClosingAsync(CancellationToken _)
        {
            eventArgs.Completed -= OnSocketReceiveCompleted;
            eventArgs?.Dispose();
            return Task.CompletedTask;
        }

        protected async Task SetTransportLevelSecurityAsync(CancellationToken cancellationToken)
        {
            if (Stream is SslStream)
            {
                return;
            }

            var sslStream = new SslStream(stream, false, ValidateRemoteCertificate);
            await sslStream.AuthenticateAsClientAsync(socket.RemoteEndPoint.ToString());
            stream = sslStream;

            return;
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

        public Action<RemoteCertificateValidationEventArgs> RemoteCertificateValidationRequired;
    }
}
