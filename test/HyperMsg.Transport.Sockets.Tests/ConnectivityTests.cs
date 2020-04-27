using System;
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
            ConnectionListener.Subscribe(context =>
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

        private async Task OpenConnectionAndAcceptContext()
        {
            await ConnectionListener.OpenAsync(default);
            await MessagingContext.Sender.SendAsync(TransportCommand.Open, default);
            acceptEvent.Wait(waitTimeout);
        }
    }
}
