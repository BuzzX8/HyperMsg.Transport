using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport
{
    /// <summary>
    /// Represents communication port (client connection or connection listener).
    /// </summary>
    public interface IPort
    {
        /// <summary>
        /// Returns true if porst opened. Otherwise false.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Opens port.
        /// </summary>
        void Open();

        /// <summary>
        /// Opens port asynchronously.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task OpenAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Closes port.
        /// </summary>
        void Close();

        /// <summary>
        /// Closes port asynchronously.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CloseAsync(CancellationToken cancellationToken);
    }
}