using System.Threading.Tasks;
using Xunit;

namespace HyperMsg.Socket
{
    [Collection("Socket.Integration")]
    public class ConnectivityTests : SocketIntegrationFixtureBase
    {
        [Fact]
        public async Task OpenTransport_Command_Establishes_Connection()
        {
            StartListening();

            await OpenTransportAsync();
            AcceptSocket();

            Assert.NotNull(AcceptedSocket);
        }
    }
}
