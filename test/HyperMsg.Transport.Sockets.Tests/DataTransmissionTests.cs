using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HyperMsg.Transport.Sockets
{
    [Collection("Socket")]
    public class DataTransmissionTests : SocketFixtureBase
    {
        private IConnectionContext acceptedConnection;
        private readonly ManualResetEventSlim acceptEvent;
        private readonly ManualResetEventSlim transmitEvent;
        private readonly TimeSpan waitTimeout;

        public DataTransmissionTests() : base(8081)
        {
            acceptEvent = new ManualResetEventSlim();
            transmitEvent = new ManualResetEventSlim();
            waitTimeout = TimeSpan.FromSeconds(2);
            ConnectionListener.Register(ac =>
            {
                acceptedConnection = ac.Acquire();
                acceptEvent.Set();
            });
        }

        [Fact(Skip = "For manual run")]
        public async Task Transmit_Sends_Data_To_Accepted_Socket()
        {
            var expectedData = Guid.NewGuid().ToByteArray();
            var actualData = default(byte[]);

            ConnectionListener.Open();
            await ClientConnection.OpenAsync(default);
            acceptEvent.Wait(waitTimeout);
            Assert.NotNull(acceptedConnection);

            acceptedConnection.BufferContext.ReceivingBuffer.FlushRequested += (reader, token) =>
            {
                actualData = reader.Read().ToArray();
                transmitEvent.Set();
                return Task.CompletedTask;
            };

            ClientContext.TransmittingBuffer.Writer.Write(expectedData);
            await ClientContext.TransmittingBuffer.FlushAsync(default);
            transmitEvent.Wait(waitTimeout);

            Assert.Equal(expectedData, actualData);
        }

        [Fact(Skip = "For manual run")]
        public async Task Receives_Data_From_Accepted_Socket()
        {
            var expectedData = Guid.NewGuid().ToByteArray();
            var actualData = default(byte[]);

            ClientContext.ReceivingBuffer.FlushRequested += (reader, token) =>
            {
                actualData = reader.Read().ToArray();
                transmitEvent.Set();
                return Task.CompletedTask;
            };

            ConnectionListener.Open();
            await ClientConnection.OpenAsync(default);
            acceptEvent.Wait(waitTimeout);
            Assert.NotNull(acceptedConnection);

            acceptedConnection.BufferContext.TransmittingBuffer.Writer.Write(expectedData);            
            await acceptedConnection.BufferContext.TransmittingBuffer.FlushAsync(default);
            transmitEvent.Wait(waitTimeout);

            Assert.Equal(expectedData, actualData);
        }
    }
}
