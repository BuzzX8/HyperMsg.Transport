using System;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITransmitter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        void Transmit(ReadOnlyMemory<byte> data);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task TransmitAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
    }
}
