using HyperMsg.Transport;
using Microsoft.Extensions.Hosting;
using System;
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

        protected override async Task TransmitDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) => await socket.SendAsync(data, SocketFlags.None, cancellationToken);

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
            if (!socket.Connected)
            {
                return;
            }

            var bytesReceived = eventArgs.BytesTransferred;

            if (bytesReceived > 0)
            {
                receivingBuffer.Writer.Advance(bytesReceived);
                ReceiveAsync(receivingBuffer, default).ContinueWith(t =>
                {
                    ResetBuffer();
                    ReceiveData();
                });

                return;
            }

            ReceiveData();
        }

        private void ResetBuffer()
        {
            var memory = receivingBuffer.Writer.GetMemory();
            eventArgs.SetBuffer(memory);
        }

        protected override Task OnConnectionClosingAsync(CancellationToken _)
        {
            eventArgs?.Dispose();
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken _) => Task.CompletedTask;

        public Task StopAsync(CancellationToken _) => Task.CompletedTask;
    }
}
