using FakeItEasy;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HyperMsg.Transport.Socket
{
    public class SocketCommandHandlerTests
    {
        private readonly SocketCommandHandler commandHandler;
        private readonly ISocket socket;
        private readonly IMessageSender messageSender;

        private readonly CancellationTokenSource tokenSource;

        public SocketCommandHandlerTests()
        {
            socket = A.Fake<ISocket>();
            messageSender = A.Fake<IMessageSender>();
            commandHandler = new SocketCommandHandler(socket, messageSender);
            tokenSource = new CancellationTokenSource();
        }

        [Fact]
        public async Task HandleCommandAsync_Calls_ConnectAsync_For_Open_Command()
        {
            await commandHandler.HandleCommandAsync(TransportCommand.Open, tokenSource.Token);

            A.CallTo(() => socket.ConnectAsync(tokenSource.Token)).MustHaveHappened();
        }

        [Fact]
        public async Task ProcessCommandAsync_Calls_DisconnectAsync_For_Close_Command()
        {
            await commandHandler.HandleCommandAsync(TransportCommand.Close, tokenSource.Token);

            A.CallTo(() => socket.DisconnectAsync(tokenSource.Token)).MustHaveHappened();
        }        

        [Fact]
        public async Task ProcessCommandAsync_Calls_SetTls_For_SetTransportLevelSecurity_Command()
        {
            await commandHandler.HandleCommandAsync(TransportCommand.SetTransportLevelSecurity, tokenSource.Token);

            A.CallTo(() => socket.SetTls()).MustHaveHappened();
        }

        [Theory]
        [InlineData(TransportCommand.Open, TransportEvent.Opening)]
        [InlineData(TransportCommand.Open, TransportEvent.Opened)]
        [InlineData(TransportCommand.Close, TransportEvent.Closing)]
        [InlineData(TransportCommand.Close, TransportEvent.Closed)]        
        private async Task HandleCommandAsync_Emits_Correct_Event(TransportCommand command, TransportEvent @event)
        {
            await commandHandler.HandleCommandAsync(command, tokenSource.Token);

            A.CallTo(() => messageSender.SendAsync(@event, tokenSource.Token)).MustHaveHappened();
        }
    }
}
