using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    internal class DataTransmissionHandler : IDisposable
    {
        private readonly ITransmitter transmitter;
        private readonly IDisposable[] subscriptions;

        public DataTransmissionHandler(IMessageObservable messageObservable, ITransmitter transmitter)
        {
            this.transmitter = transmitter ?? throw new ArgumentNullException(nameof(transmitter));
            subscriptions = new[]
            {
                messageObservable.OnBufferDataTransmit(HandleAsync),
                messageObservable.OnTransmit<byte[]>(HandleAsync),
                messageObservable.OnTransmit<Memory<byte>>(HandleAsync),
                messageObservable.OnTransmit<ReadOnlyMemory<byte>>(HandleAsync)
            };
        }

        public async Task HandleAsync(IBuffer transmittingBuffer, CancellationToken cancellationToken)
        {
            var reader = transmittingBuffer.Reader;
            var buffer = reader.Read();

            if (buffer.Length == 0)
            {
                return;
            }

            if (buffer.IsSingleSegment)
            {
                await transmitter.TransmitAsync(buffer.First, cancellationToken);
                reader.Advance((int)buffer.Length);
                return;
            }

            var enumerator = buffer.GetEnumerator();

            while (enumerator.MoveNext())
            {
                await transmitter.TransmitAsync(enumerator.Current, cancellationToken);
            }
        }

        private Task HandleAsync(byte[] buffer, CancellationToken cancellationToken) => transmitter.TransmitAsync(buffer, cancellationToken);

        private Task HandleAsync(Memory<byte> buffer, CancellationToken cancellationToken) => transmitter.TransmitAsync(buffer, cancellationToken);

        private Task HandleAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) => transmitter.TransmitAsync(buffer, cancellationToken);

        public void Dispose()
        {
            foreach(var subscription in subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}
