using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport.Socket
{
    public class SocketTransport : ITransport
    {
        private readonly ISocket socket;
        private readonly IBufferWriter<byte> bufferWriter;
        private readonly AsyncAction flushHandler;

        public SocketTransport(ISocket socket, IBufferWriter<byte> bufferWriter, AsyncAction flushHandler)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            this.bufferWriter = bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter));
            this.flushHandler = flushHandler ?? throw new ArgumentNullException(nameof(flushHandler));
        }

        public Task ProcessCommandAsync(TransportCommand command, CancellationToken cancellationToken)
        {
            switch (command)
            {
                case TransportCommand.Open:
                    return OpenAsync(cancellationToken);

                case TransportCommand.Close:
                    return CloseAsync(cancellationToken);

                case TransportCommand.SetTransportLevelSecurity:
                    socket.SetTls();
                    break;
            }

            return Task.CompletedTask;
        }

        public Task DoSocketReadingAsync(CancellationToken cancellationToken)
        {
            DoSocketReading();
            return flushHandler.Invoke(cancellationToken);
        }

        public async Task HandleFlushRequestAsync(IBufferReader<byte> bufferReader, CancellationToken cancellationToken)
        {
            var ros = bufferReader.Read();
            var enumerator = ros.GetEnumerator();

            while (enumerator.MoveNext())
            {
                await socket.Stream.WriteAsync(enumerator.Current, cancellationToken);
            }

            bufferReader.Advance((int)ros.Length);
        }

        private async Task OpenAsync(CancellationToken cancellationToken)
        {
            await OnTransportEventAsync(HyperMsg.TransportEvent.Opening, cancellationToken);
            await socket.ConnectAsync(cancellationToken);
            await OnTransportEventAsync(HyperMsg.TransportEvent.Opened, cancellationToken);
        }

        private async Task CloseAsync(CancellationToken cancellationToken)
        {
            await OnTransportEventAsync(HyperMsg.TransportEvent.Closing, cancellationToken);
            await socket.DisconnectAsync(cancellationToken);
            await OnTransportEventAsync(HyperMsg.TransportEvent.Closed, cancellationToken);
        }

        private void DoSocketReading()
        {
            var memory = bufferWriter.GetMemory();
            var bytesReaded = socket.Stream.Read(memory.Span);

            bufferWriter.Advance(bytesReaded);
        }

        private Task OnTransportEventAsync(TransportEvent @event, CancellationToken cancellationToken)
        {
            if (TransportEvent != null)
            {
                return TransportEvent.Invoke(new TransportEventArgs(@event), cancellationToken);
            }

            return Task.CompletedTask;
        }

        public event AsyncAction<TransportEventArgs> TransportEvent;
    }
}
