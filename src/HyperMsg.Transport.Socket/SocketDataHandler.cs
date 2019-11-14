using System;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport.Socket
{
    public class SocketDataHandler
    {
        private readonly ISocket socket;
        private readonly IBuffer receivingBuffer;

        public SocketDataHandler(ISocket socket, IBuffer receivingBuffer)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            this.receivingBuffer = receivingBuffer ?? throw new ArgumentNullException(nameof(receivingBuffer));
        }

        public Task FetchSocketDataAsync(CancellationToken cancellationToken)
        {
            DoSocketReading();
            return receivingBuffer.FlushAsync(cancellationToken);
        }

        private void DoSocketReading()
        {
            var memory = receivingBuffer.Writer.GetMemory();
            var bytesReaded = socket.Stream.Read(memory.Span);

            receivingBuffer.Writer.Advance(bytesReaded);
        }

        public async Task HandleBufferFlushAsync(IBufferReader<byte> bufferReader, CancellationToken cancellationToken)
        {
            var ros = bufferReader.Read();
            var enumerator = ros.GetEnumerator();

            while (enumerator.MoveNext())
            {
                await socket.Stream.WriteAsync(enumerator.Current, cancellationToken);
            }

            bufferReader.Advance((int)ros.Length);
        }
    }
}
