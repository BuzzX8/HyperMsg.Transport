using System;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    public class ConnectionCommandHandler
    {
        private readonly IPort connection;
        private readonly IMessageSender messageSender;

        public ConnectionCommandHandler(IPort connection, IMessageSender messageSender)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
        }

        public void Handle(TransportCommand transportCommand)
        {
            switch (transportCommand)
            {
                case TransportCommand.Open:
                    messageSender.Send(TransportEvent.Opening);
                    connection.Open();
                    messageSender.Send(TransportEvent.Opened);
                    break;

                case TransportCommand.Close:
                    messageSender.Send(TransportEvent.Closing);
                    connection.Close();
                    messageSender.Send(TransportEvent.Closed);
                    break;
            }
        }

        public async Task HandleAsync(TransportCommand transportCommand, CancellationToken cancellationToken)
        {
            switch (transportCommand)
            {
                case TransportCommand.Open:
                    await messageSender.SendAsync(TransportEvent.Opening, cancellationToken);
                    await connection.OpenAsync(cancellationToken);
                    await messageSender.SendAsync(TransportEvent.Opened, cancellationToken);
                    break;

                case TransportCommand.Close:
                    await messageSender.SendAsync(TransportEvent.Closing, cancellationToken);
                    await connection.CloseAsync(cancellationToken);
                    await messageSender.SendAsync(TransportEvent.Closed, cancellationToken);
                    break;
            }
        }
    }
}
