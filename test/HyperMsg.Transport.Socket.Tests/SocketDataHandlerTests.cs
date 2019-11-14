using FakeItEasy;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HyperMsg.Transport.Socket
{
    public class SocketDataHandlerTests
    {
        private readonly SocketDataHandler dataHandler;
        private readonly ISocket socket;
        private readonly IBuffer buffer;

        private readonly CancellationTokenSource tokenSource;

        public SocketDataHandlerTests()
        {
            socket = A.Fake<ISocket>();
            buffer = A.Fake<IBuffer>();
            dataHandler = new SocketDataHandler(socket, buffer);
            tokenSource = new CancellationTokenSource();
        }

        [Fact]
        public async Task FetchSocketDataAsync_Reads_Data_From_Stream_And_Writes_Into_Buffer()
        {
            var data = Guid.NewGuid().ToByteArray();
            var stream = new MemoryStream(data);
            var bytes = new byte[1024];
            A.CallTo(() => buffer.Writer.GetMemory(0)).Returns(bytes);
            A.CallTo(() => socket.Stream).Returns(stream);

            await dataHandler.FetchSocketDataAsync(tokenSource.Token);

            Assert.Equal(stream.ToArray(), bytes.Take(data.Length).ToArray());
            A.CallTo(() => buffer.Writer.Advance(data.Length)).MustHaveHappened();
            A.CallTo(() => buffer.FlushAsync(tokenSource.Token)).MustHaveHappened();
        }

        [Fact]
        public async Task HandleBufferFlushAsync_Writes_Data_Into_Stream()
        {
            var data = Guid.NewGuid().ToByteArray();
            var stream = new MemoryStream();
            A.CallTo(() => socket.Stream).Returns(stream);
            var reader = A.Fake<IBufferReader<byte>>();
            A.CallTo(() => reader.Read()).Returns(new ReadOnlySequence<byte>(data));

            await dataHandler.HandleBufferFlushAsync(reader, tokenSource.Token);

            Assert.Equal(data, stream.ToArray());
            A.CallTo(() => reader.Advance(data.Length)).MustHaveHappened();
        }
    }
}
