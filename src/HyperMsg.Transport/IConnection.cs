using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    /// <summary>
    /// 
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// 
        /// </summary>
        void Connect();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ConnectAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DisconnectAsync(CancellationToken cancellationToken);
    }
}