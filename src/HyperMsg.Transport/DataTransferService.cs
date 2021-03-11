using HyperMsg.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    public abstract class DataTransferService : MessagingService
    {
        protected DataTransferService(IMessagingContext messagingContext) : base(messagingContext)
        {
        }

        protected override IEnumerable<IDisposable> GetDefaultDisposables()
        {
            yield return this.RegisterTransmitHandler<byte[]>(TransmitDataAsync);
            yield return this.RegisterTransmitHandler<ArraySegment<byte>>(TransmitDataAsync);
            yield return this.RegisterTransmitHandler<ReadOnlyMemory<byte>>(TransmitDataAsync);
                         
            yield return this.RegisterHandler(ConnectionEvent.Opening, OnConnectionOpeningAsync);
            yield return this.RegisterHandler(ConnectionEvent.Opened, OnConnectionOpenedAsync);
            yield return this.RegisterHandler(ConnectionEvent.Closing, OnConnectionClosingAsync);
            yield return this.RegisterHandler(ConnectionEvent.Closed, OnConnectionClosedAsync);
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
