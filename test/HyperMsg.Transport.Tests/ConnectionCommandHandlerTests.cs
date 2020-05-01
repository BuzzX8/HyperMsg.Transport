using FakeItEasy;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HyperMsg.Transport
{
    public class ConnectionCommandHandlerTests
    {
        private readonly ConnectionCommandHandler commandHandler;
        private readonly IMessagingContext messagingContext;
        private readonly IPort port;

        private readonly CancellationTokenSource tokenSource;

        public ConnectionCommandHandlerTests()
        {
            port = A.Fake<IPort>();
            messagingContext = A.Fake<IMessagingContext>();
            commandHandler = new ConnectionCommandHandler(messagingContext, port);
            tokenSource = new CancellationTokenSource();
        }

        [Fact]
        public async Task HandleAsync_Calls_ConnectAsync_For_Open_Command()
        {
            await commandHandler.HandleAsync(TransportCommand.Open, tokenSource.Token);

            A.CallTo(() => port.OpenAsync(tokenSource.Token)).MustHaveHappened();
        }

        [Fact]
        public async Task ProcessCommandAsync_Calls_DisconnectAsync_For_Close_Command()
        {
            await commandHandler.HandleAsync(TransportCommand.Close, tokenSource.Token);

            A.CallTo(() => port.CloseAsync(tokenSource.Token)).MustHaveHappened();
        }

        [Theory]
        [InlineData(TransportCommand.Open, TransportEvent.Opening)]
        [InlineData(TransportCommand.Open, TransportEvent.Opened)]
        [InlineData(TransportCommand.Close, TransportEvent.Closing)]
        [InlineData(TransportCommand.Close, TransportEvent.Closed)]        
        public async Task HandleCommandAsync_Emits_Correct_Event(TransportCommand command, TransportEvent @event)
        {
            await commandHandler.HandleAsync(command, tokenSource.Token);

            A.CallTo(() => messagingContext.Sender.SendAsync(@event, tokenSource.Token)).MustHaveHappened();
        }
    }
}
