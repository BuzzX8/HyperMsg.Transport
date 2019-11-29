using System;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    public interface IReceiver
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer">Receiving buffer</param>
        /// <returns>Bytes readed</returns>
        int Receive(Memory<byte> buffer);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Bytes readed</returns>
        Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    }
}
