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
    public class SocketTransportTests
    {
        private readonly ISocket socket;
        private readonly IBufferWriter<byte> bufferWriter;
        private readonly AsyncAction flushHandler;
        private readonly SocketTransport socketTransport;

        private readonly CancellationTokenSource tokenSource;

        public SocketTransportTests()
        {
            socket = A.Fake<ISocket>();
            bufferWriter = A.Fake<IBufferWriter<byte>>();
            flushHandler = A.Fake<AsyncAction>();
            socketTransport = new SocketTransport(socket, bufferWriter, flushHandler);
            tokenSource = new CancellationTokenSource();
        }

        [Fact]
        public async Task ProcessCommandAsync_Calls_ConnectAsync_For_Open_Command()
        {
            await socketTransport.ProcessCommandAsync(TransportCommand.Open, tokenSource.Token);

            A.CallTo(() => socket.ConnectAsync(tokenSource.Token)).MustHaveHappened();
        }

        [Fact]
        public async Task ProcessCommandAsync_Rises_Opening_Event()
        {
            var eventArgs = default(TransportEventArgs);
            socketTransport.TransportEvent += (e, t) =>
            {
                if (e.Event == TransportEvent.Opening)
                {
                    eventArgs = e;
                    A.CallTo(() => socket.ConnectAsync(A<CancellationToken>._)).MustNotHaveHappened();
                }
                return Task.CompletedTask;
            };

            await socketTransport.ProcessCommandAsync(TransportCommand.Open, tokenSource.Token);

            Assert.NotNull(eventArgs);
            Assert.Equal(TransportEvent.Opening, eventArgs.Event);
        }

        [Fact]
        public async Task ProcessCommandAsync_Rises_Opened_Event()
        {
            var eventArgs = default(TransportEventArgs);
            socketTransport.TransportEvent += (e, t) =>
            {
                if (e.Event == TransportEvent.Opened)
                {
                    eventArgs = e;
                    A.CallTo(() => socket.ConnectAsync(A<CancellationToken>._)).MustHaveHappened();
                }
                return Task.CompletedTask;
            };

            await socketTransport.ProcessCommandAsync(TransportCommand.Open, tokenSource.Token);

            Assert.NotNull(eventArgs);
            Assert.Equal(TransportEvent.Opened, eventArgs.Event);
        }

        [Fact]
        public async Task ProcessCommandAsync_Calls_DisconnectAsync_For_Close_Command()
        {
            await socketTransport.ProcessCommandAsync(TransportCommand.Close, tokenSource.Token);

            A.CallTo(() => socket.DisconnectAsync(tokenSource.Token)).MustHaveHappened();
        }

        [Fact]
        public async Task ProcessCommandAsync_Rises_Closing_Event()
        {
            var eventArgs = default(TransportEventArgs);
            socketTransport.TransportEvent += (e, t) =>
            {
                if (e.Event == TransportEvent.Closing)
                {
                    eventArgs = e;
                    A.CallTo(() => socket.DisconnectAsync(A<CancellationToken>._)).MustNotHaveHappened();
                }
                return Task.CompletedTask;
            };

            await socketTransport.ProcessCommandAsync(TransportCommand.Close, tokenSource.Token);

            Assert.NotNull(eventArgs);
            Assert.Equal(TransportEvent.Closing, eventArgs.Event);
        }

        [Fact]
        public async Task ProcessCommandAsync_Rises_Closed_Event()
        {
            var eventArgs = default(TransportEventArgs);
            socketTransport.TransportEvent += (e, t) =>
            {
                if (e.Event == TransportEvent.Closed)
                {
                    eventArgs = e;
                    A.CallTo(() => socket.DisconnectAsync(A<CancellationToken>._)).MustHaveHappened();
                }
                return Task.CompletedTask;
            };

            await socketTransport.ProcessCommandAsync(TransportCommand.Close, tokenSource.Token);

            Assert.NotNull(eventArgs);
            Assert.Equal(TransportEvent.Closed, eventArgs.Event);
        }

        [Fact]
        public async Task ProcessCommandAsync_Calls_SetTls_For_SetTransportLevelSecurity_Command()
        {
            await socketTransport.ProcessCommandAsync(TransportCommand.SetTransportLevelSecurity, tokenSource.Token);

            A.CallTo(() => socket.SetTls()).MustHaveHappened();
        }

        [Fact]
        public async Task DoSocketReadingAsync_Reads_Data_From_Stream_And_Writes_Into_Buffer()
        {
            var data = Guid.NewGuid().ToByteArray();
            var stream = new MemoryStream(data);
            var buffer = new byte[1024];
            A.CallTo(() => bufferWriter.GetMemory(0)).Returns(buffer);            
            A.CallTo(() => socket.Stream).Returns(stream);

            await socketTransport.DoSocketReadingAsync(tokenSource.Token);

            Assert.Equal(stream.ToArray(), buffer.Take(data.Length).ToArray());
            A.CallTo(() => bufferWriter.Advance(data.Length)).MustHaveHappened();
            A.CallTo(() => flushHandler.Invoke(tokenSource.Token)).MustHaveHappened();
        }

        [Fact]
        public async Task HandleFlushRequestAsync_Writes_Data_Into_Stream()
        {
            var data = Guid.NewGuid().ToByteArray();
            var stream = new MemoryStream();            
            A.CallTo(() => socket.Stream).Returns(stream);
            var reader = A.Fake<IBufferReader<byte>>();
            A.CallTo(() => reader.Read()).Returns(new ReadOnlySequence<byte>(data));

            await socketTransport.HandleFlushRequestAsync(reader, tokenSource.Token);

            Assert.Equal(data, stream.ToArray());
            A.CallTo(() => reader.Advance(data.Length)).MustHaveHappened();
        }
    }
}
