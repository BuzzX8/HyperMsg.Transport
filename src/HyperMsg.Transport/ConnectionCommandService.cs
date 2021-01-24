using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    public abstract class ConnectionCommandService : MessagingObject, IHostedService
    {
        protected ConnectionCommandService(IMessagingContext messagingContext) : base(messagingContext)
        {
            AddHandler<TransportCommand>(HandleAsync);
        }

        private async Task HandleAsync(TransportCommand transportCommand, CancellationToken cancellationToken)
        {
            switch (transportCommand)
            {
                case TransportCommand.Open:
                    await SendAsync(TransportEvent.Opening, cancellationToken);
                    await OpenAsync(cancellationToken);
                    await SendAsync(TransportEvent.Opened, cancellationToken);
                    break;

                case TransportCommand.Close:
                    await SendAsync(TransportEvent.Closing, cancellationToken);
                    await CloseAsync(cancellationToken);
                    await SendAsync(TransportEvent.Closed, cancellationToken);
                    break;
            }
        }

        protected abstract Task OpenAsync(CancellationToken cancellationToken);

        protected abstract Task CloseAsync(CancellationToken cancellationToken);

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
