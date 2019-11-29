using System;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    internal class DataTransmissionHandler
    {
        private readonly ITransmitter transmitter;

        public DataTransmissionHandler(ITransmitter transmitter)
        {
            this.transmitter = transmitter ?? throw new ArgumentNullException(nameof(transmitter));
        }

        public async Task HandleBufferFlushAsync(IBufferReader<byte> reader, CancellationToken cancellationToken)
        {
            var buffer = reader.Read();

            if (buffer.Length == 0)
            {
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
