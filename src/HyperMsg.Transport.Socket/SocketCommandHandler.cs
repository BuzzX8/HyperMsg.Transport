using System;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport.Socket
{
    public class SocketCommandHandler
    {
        private readonly ISocket socket;
        private readonly IMessageSender messageSender;

        public SocketCommandHandler(ISocket socket, IMessageSender messageSender)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            this.messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
        }

        public Task HandleCommandAsync(TransportCommand command, CancellationToken cancellationToken)
        {
            switch (command)
            {
                case TransportCommand.Open:
                    return OpenAsync(cancellationToken);

                case TransportCommand.Close:
                    return CloseAsync(cancellationToken);

                case TransportCommand.SetTransportLevelSecurity:
                    socket.SetTls();
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task OpenAsync(CancellationToken cancellationToken)
        {
            await OnTransportEventAsync(TransportEvent.Opening, cancellationToken);
            await socket.ConnectAsync(cancellationToken);
            await OnTransportEventAsync(TransportEvent.Opened, cancellationToken);
        }

        private async Task CloseAsync(CancellationToken cancellationToken)
        {
            await OnTransportEventAsync(TransportEvent.Closing, cancellationToken);
            await socket.DisconnectAsync(cancellationToken);
            await OnTransportEventAsync(TransportEvent.Closed, cancellationToken);
        }

        private Task OnTransportEventAsync(TransportEvent @event, CancellationToken cancellationToken) => messageSender.SendAsync(@event, cancellationToken);
    }
}
