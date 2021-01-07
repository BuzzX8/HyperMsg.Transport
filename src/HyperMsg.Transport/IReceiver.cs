using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    /// <summary>
    /// Represents data receiver.
    /// </summary>
    public interface IReceiver
    {
        /// <summary>
        /// Receives data into buffer.
        /// </summary>
        /// <param name="buffer">Memory buffer</param>
        /// <returns>Bytes readed</returns>
        int Receive(IBufferWriter<byte> buffer);

        /// <summary>
        /// Receives data into buffer asynchronously.
        /// </summary>
        /// <param name="buffer">Memory buffer</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Bytes readed</returns>
        Task<int> ReceiveAsync(IBufferWriter<byte> buffer, CancellationToken cancellationToken);
    }
}
