using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HyperMsg.Transport.Sockets
{
    [Collection("Socket")]    
    public class ConnectivityTests : SocketFixtureBase
    {
        private IConnectionContext acceptedContext;
        private readonly ManualResetEventSlim acceptEvent;
        private readonly TimeSpan waitTimeout;

        public ConnectivityTests() : base(8080)
        {
            acceptEvent = new ManualResetEventSlim();
            waitTimeout = TimeSpan.FromSeconds(2);
            ConnectionListener.Register(context =>
            {
                acceptedContext = context.Acquire();
                acceptEvent.Set();
            });
        }

        [Fact(Skip = "For manual run")]
        public async Task Opens_Connection_And_Accepts_Socket()
        {
            await OpenConnectionAndAcceptContext();

            Assert.NotNull(acceptedContext);
        }

        [Fact(Skip = "For manual run")]
        public async Task Close_Disables_Data_Transmission_From_Client_Side()
        {
            await OpenConnectionAndAcceptContext();

            ClientConnection.Close();

            ClientContext.TransmittingBuffer.Writer.Write(Guid.NewGuid().ToByteArray());
            await Assert.ThrowsAsync<InvalidOperationException>(() => ClientContext.TransmittingBuffer.FlushAsync(default));
        }

        private async Task OpenConnectionAndAcceptContext()
        {
            await ConnectionListener.OpenAsync(default);
            await ClientConnection.OpenAsync(default);
            acceptEvent.Wait(waitTimeout);
        }
    }
}
