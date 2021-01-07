using System;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    internal class ConnectionCommandComponent
    {
        private readonly IMessageSender messageSender;        
        private readonly IPort port;        

        public ConnectionCommandComponent(IMessageSender messageSender, IPort port)
        {
            this.port = port ?? throw new ArgumentNullException(nameof(port));
            this.messageSender = messageSender;
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
    }
}
