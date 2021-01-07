using System;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    internal class BufferDataTransmissionComponent
    {
        private readonly ITransmitter transmitter;

        public BufferDataTransmissionComponent(ITransmitter transmitter)
        {
            this.transmitter = transmitter ?? throw new ArgumentNullException(nameof(transmitter));
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
    }
}
