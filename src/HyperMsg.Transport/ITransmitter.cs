using System;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    /// <summary>
    /// Represents data transmitter.
    /// </summary>
    public interface ITransmitter
    {
        /// <summary>
        /// Transmits data from buffer.
        /// </summary>
        /// <param name="data">Memory bufer.</param>
        void Transmit(ReadOnlyMemory<byte> data);

        /// <summary>
        /// Transmits data from buffer asynchronously.
        /// </summary>
        /// <param name="data">Memory bufer.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task TransmitAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
    }
}
