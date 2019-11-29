using FakeItEasy;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HyperMsg.Transport
{
    public class ConnectionCommandHandlerTests
    {
        private readonly ConnectionCommandHandler commandHandler;
        private readonly IConnection connection;
        private readonly IMessageSender messageSender;

        private readonly CancellationTokenSource tokenSource;

        public ConnectionCommandHandlerTests()
        {
            connection = A.Fake<IConnection>();
            messageSender = A.Fake<IMessageSender>();
            commandHandler = new ConnectionCommandHandler(connection, messageSender);
            tokenSource = new CancellationTokenSource();
        }

        [Fact]
        public async Task HandleAsync_Calls_ConnectAsync_For_Open_Command()
        {
            await commandHandler.HandleAsync(TransportCommand.Open, tokenSource.Token);

            A.CallTo(() => connection.ConnectAsync(tokenSource.Token)).MustHaveHappened();
        }

        [Fact]
        public async Task ProcessCommandAsync_Calls_DisconnectAsync_For_Close_Command()
        {
            await commandHandler.HandleAsync(TransportCommand.Close, tokenSource.Token);

            A.CallTo(() => connection.DisconnectAsync(tokenSource.Token)).MustHaveHappened();
        }

        [Theory]
        [InlineData(TransportCommand.Open, TransportEvent.Opening)]
        [InlineData(TransportCommand.Open, TransportEvent.Opened)]
        [InlineData(TransportCommand.Close, TransportEvent.Closing)]
        [InlineData(TransportCommand.Close, TransportEvent.Closed)]        
        private async Task HandleCommandAsync_Emits_Correct_Event(TransportCommand command, TransportEvent @event)
        {
            await commandHandler.HandleAsync(command, tokenSource.Token);

            A.CallTo(() => messageSender.SendAsync(@event, tokenSource.Token)).MustHaveHappened();
        }
    }
}
