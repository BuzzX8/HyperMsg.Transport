using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    public class ConnectionCommandHandler
    {
        private readonly IConnection connection;
        private readonly IMessageSender messageSender;

        public ConnectionCommandHandler(IConnection connection, IMessageSender messageSender)
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
                    connection.Connect();
                    messageSender.Send(TransportEvent.Opened);
                    break;

                case TransportCommand.Close:
                    messageSender.Send(TransportEvent.Closing);
                    connection.Disconnect();
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
                    await connection.ConnectAsync(cancellationToken);
                    await messageSender.SendAsync(TransportEvent.Opened, cancellationToken);
                    break;

                case TransportCommand.Close:
                    await messageSender.SendAsync(TransportEvent.Closing, cancellationToken);
                    await connection.DisconnectAsync(cancellationToken);
                    await messageSender.SendAsync(TransportEvent.Closed, cancellationToken);
                    break;
            }
        }
    }
}
