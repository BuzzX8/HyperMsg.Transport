using System;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    internal class DataTransmissionHandler : IDisposable
    {
        private readonly ITransmitter transmitter;
        private readonly IDisposable subscription;

        public DataTransmissionHandler(IMessageObservable messageObservable, ITransmitter transmitter)
        {
            this.transmitter = transmitter ?? throw new ArgumentNullException(nameof(transmitter));
            subscription = messageObservable.OnBufferDataTransmit(HandleBufferDataTransmit);
        }

        public async Task HandleBufferDataTransmit(IBuffer transmittingBuffer, CancellationToken cancellationToken)
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

        public void Dispose() => subscription.Dispose();
    }
}
