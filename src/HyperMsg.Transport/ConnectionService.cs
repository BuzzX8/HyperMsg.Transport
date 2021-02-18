using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    public abstract class ConnectionService : MessagingObject
    {
        public ConnectionService(IMessagingContext messagingContext) : base(messagingContext)
        {
            RegisterHandler(ConnectionCommand.Open, OpenConnection);
            RegisterHandler(ConnectionCommand.Close, CloseConnection);
            RegisterHandler(ConnectionCommand.SetTransportLevelSecurity, SetTransportLevelSecurityAsync);
        }

        private async Task OpenConnection(CancellationToken cancellationToken)
        {
            await SendAsync(ConnectionEvent.Opening, cancellationToken);
            await OpenConnectionAsync(cancellationToken);
            await SendAsync(ConnectionEvent.Opened, cancellationToken);
        }

        private async Task CloseConnection(CancellationToken cancellationToken)
        {
            await SendAsync(ConnectionEvent.Closing, cancellationToken);
            await CloseConnectionAsync(cancellationToken);
            await SendAsync(ConnectionEvent.Closed, cancellationToken);
        }

        protected abstract Task OpenConnectionAsync(CancellationToken cancellationToken);

        protected abstract Task CloseConnectionAsync(CancellationToken cancellationToken);

        protected virtual Task SetTransportLevelSecurityAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
