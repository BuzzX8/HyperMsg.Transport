using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Socket
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

        private IBufferWriter<byte> BufferWriter => receivingBuffer.Writer;

        public async Task FetchSocketDataAsync(CancellationToken cancellationToken)
        {
            var memory = BufferWriter.GetMemory();
            var bytesReaded = await socket.Stream.ReadAsync(memory);
            BufferWriter.Advance(bytesReaded);
            await receivingBuffer.FlushAsync(cancellationToken);
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
