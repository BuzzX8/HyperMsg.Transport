using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HyperMsg.Socket
{
    [Collection("Socket.Integration")]
    public class DataTransmissionTests : SocketIntegrationFixtureBase
    {
        [Fact]
        public async Task Transmit_Sends_Data_To_Accepted_Socket()
        {
            var transmittingBuffer = GetService<ITransmittingBuffer>();
            HandlerRegistry.Register<Transmit<byte[]>>(async d =>
            {
                transmittingBuffer.Writer.Write(new ReadOnlySpan<byte>(d));
                await transmittingBuffer.FlushAsync(CancellationToken.None);
            });

            await StartListeningAndAcceptSocket();
            var data = Guid.NewGuid().ToByteArray();

            await MessageSender.TransmitAsync(data, CancellationToken.None);

            var actualData = new byte[data.Length];
            AcceptedSocket?.Receive(actualData);

            Assert.Equal(data, actualData);
        }

        [Fact]
        public async Task Receives_Data_From_Accepted_Socket()
        {
            var receivedData = Array.Empty<byte>();
            var receivingBuffer = GetService<IReceivingBuffer>();
            var receiveEvent = new ManualResetEventSlim();
            receivingBuffer.FlushRequested += (r, t) =>
            {
                receivedData = r.Read().ToArray();
                receiveEvent.Set();
                return Task.CompletedTask;
            };

            await StartListeningAndAcceptSocket();
            var data = Guid.NewGuid().ToByteArray();

            AcceptedSocket.Send(data);
            receiveEvent.Wait(TimeSpan.FromSeconds(2));

            Assert.Equal(data, receivedData);
        }

        private async Task StartListeningAndAcceptSocket()
        {
            StartListening();
            await OpenTransportAsync();
            AcceptSocket();
        }
    }
}
