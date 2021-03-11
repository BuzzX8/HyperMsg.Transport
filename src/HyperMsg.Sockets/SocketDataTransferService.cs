using HyperMsg.Extensions;
using HyperMsg.Transport;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Sockets
{
    internal class SocketDataTransferService : DataTransferService, IHostedService
    {
        private readonly IBuffer receivingBuffer;
        private readonly Socket socket;

        private SocketAsyncEventArgs eventArgs;

        public SocketDataTransferService(Socket socket, IBuffer receivingBuffer, IMessagingContext messagingContext) : base(messagingContext)
        {            
            this.receivingBuffer = receivingBuffer;
            this.socket = socket;
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
            var bytesReceived = eventArgs.BytesTransferred;

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
            receivingBuffer.Clear();
            var memory = receivingBuffer.Writer.GetMemory();
            eventArgs.SetBuffer(memory);
        }

        protected override Task OnConnectionClosingAsync(CancellationToken _)
        {
            eventArgs.Completed -= OnSocketReceiveCompleted;
            eventArgs?.Dispose();
            return Task.CompletedTask;
        }
    }
}
