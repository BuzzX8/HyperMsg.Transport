using System;
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
        private readonly TimeSpan waitTimeout;

        public DataTransmissionTests() : base(8081)
        {
            acceptEvent = new ManualResetEventSlim();
            waitTimeout = TimeSpan.FromSeconds(2);
            ConnectionListener.Subscribe(ac =>
            {
                acceptedConnection = ac.Acquire();
                acceptEvent.Set();
            });
        }

        [Fact(Skip = "For manual run")]
        public async Task Transmit_Sends_Data_To_Accepted_Socket()
        {
            var expectedData = Guid.NewGuid().ToByteArray();
            var actualData = new byte[expectedData.Length];

            ConnectionListener.Open();
            await MessagingContext.Sender.SendAsync(TransportCommand.Open, default);
            acceptEvent.Wait(waitTimeout);
            Assert.NotNull(acceptedConnection);
            
            await MessagingContext.Sender.TransmitAsync(expectedData, default);
            await acceptedConnection.Receiver.ReceiveAsync(actualData, default);

            Assert.Equal(expectedData, actualData);
        }
    }
}
