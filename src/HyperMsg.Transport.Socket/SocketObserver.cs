using System;
using System.Net.Sockets;
using System.Threading;

namespace HyperMsg.Socket
{
    public class SocketObserver
    {
        private readonly IBuffer buffer;
        private readonly System.Net.Sockets.Socket socket;
        private readonly SocketAsyncEventArgs socketEventArgs;

        public SocketObserver(IBuffer buffer, System.Net.Sockets.Socket socket)
        {
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            socketEventArgs = new SocketAsyncEventArgs();
            socketEventArgs.Completed += DataReceivingCompleted;
        }

        public void Handle(TransportEvent transportEvent)
        {
            switch (transportEvent)
            {
                case TransportEvent.Opened:
                    StartListening();
                    break;
            }
        }

        private void StartListening()
        {
            ResetBuffer();
            while (!socket.ReceiveAsync(socketEventArgs))
            {
                AdvanceAndFlushBuffer();
                ResetBuffer();
            }
        }

        private void DataReceivingCompleted(object sender, SocketAsyncEventArgs e)
        {
            AdvanceAndFlushBuffer();
            ResetBuffer();
        }

        private void AdvanceAndFlushBuffer()
        {
            if (socketEventArgs.BytesTransferred == 0)
            {
                return;
            }
          
            buffer.Writer.Advance(socketEventArgs.BytesTransferred);
            buffer.FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        private void ResetBuffer()
        {
            var memory = buffer.Writer.GetMemory();
            socketEventArgs.SetBuffer(memory);
        }
    }
}
