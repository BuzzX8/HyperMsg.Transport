using System;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    public abstract class DataTransferService : MessagingObject
    {
        protected DataTransferService(IMessagingContext messagingContext) : base(messagingContext)
        {
            RegisterTransmitHandler<byte[]>(TransmitDataAsync);
            RegisterTransmitHandler<ArraySegment<byte>>(TransmitDataAsync);
            RegisterTransmitHandler<ReadOnlyMemory<byte>>(TransmitDataAsync);

            RegisterHandler(ConnectionEvent.Opening, OnConnectionOpeningAsync);
            RegisterHandler(ConnectionEvent.Opened, OnConnectionOpenedAsync);
            RegisterHandler(ConnectionEvent.Closing, OnConnectionClosingAsync);
            RegisterHandler(ConnectionEvent.Closed, OnConnectionClosedAsync);
        }

        private Task TransmitDataAsync(byte[] data, CancellationToken cancellationToken) => TransmitDataAsync(new ReadOnlyMemory<byte>(data), cancellationToken);

        private Task TransmitDataAsync(ArraySegment<byte> data, CancellationToken cancellationToken) => TransmitDataAsync(data.AsMemory(), cancellationToken);

        protected abstract Task TransmitDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

        protected virtual Task OnConnectionOpeningAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected virtual Task OnConnectionOpenedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected virtual Task OnConnectionClosingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected virtual Task OnConnectionClosedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
