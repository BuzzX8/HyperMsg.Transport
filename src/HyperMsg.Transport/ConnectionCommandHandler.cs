using System;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    public class ConnectionCommandHandler : IDisposable
    {
        private readonly IMessageSender messageSender;
        private readonly IDisposable subscription;
        private readonly IPort port;        

        public ConnectionCommandHandler(IMessagingContext messagingContext, IPort port)
        {
            this.port = port ?? throw new ArgumentNullException(nameof(port));
            messageSender = messagingContext.Sender;
            subscription = messagingContext.Observable.Subscribe<TransportCommand>(HandleAsync);
        }

        public async Task HandleAsync(TransportCommand transportCommand, CancellationToken cancellationToken)
        {
            switch (transportCommand)
            {
                case TransportCommand.Open:
                    await messageSender.SendAsync(TransportEvent.Opening, cancellationToken);
                    await port.OpenAsync(cancellationToken);
                    await messageSender.SendAsync(TransportEvent.Opened, cancellationToken);
                    break;

                case TransportCommand.Close:
                    await messageSender.SendAsync(TransportEvent.Closing, cancellationToken);
                    await port.CloseAsync(cancellationToken);
                    await messageSender.SendAsync(TransportEvent.Closed, cancellationToken);
                    break;
            }
        }

        public void Dispose() => subscription.Dispose();
    }
}
