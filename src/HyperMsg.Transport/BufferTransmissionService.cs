using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    internal class BufferTransmissionService : MessagingObject, IHostedService
    {
        private readonly BufferTransmitters transmitters;

        public BufferTransmissionService(BufferTransmitters transmitters, IMessagingContext messagingContext) : base(messagingContext)
        {
            this.transmitters = transmitters;
            AddTransmitter<IBuffer>(HandleAsync);
        }

        private async Task HandleAsync(IBuffer transmittingBuffer, CancellationToken cancellationToken)
        {
            var reader = transmittingBuffer.Reader;
            var buffer = reader.Read();

            if (buffer.Length == 0)
            {
                return;
            }

            if (buffer.IsSingleSegment)
            {
                await TransmitDataAsync(buffer.First, cancellationToken);
                reader.Advance((int)buffer.Length);
                return;
            }

            var enumerator = buffer.GetEnumerator();

            while (enumerator.MoveNext())
            {
                await TransmitDataAsync(enumerator.Current, cancellationToken);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task TransmitDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            foreach(var transmitter in transmitters)
            {
                await transmitter.Invoke(data, cancellationToken);
            }
        }
    }

    internal class BufferTransmitters : List<AsyncAction<ReadOnlyMemory<byte>>> { }
}
