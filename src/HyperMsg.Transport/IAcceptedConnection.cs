namespace HyperMsg.Transport
{
    /// <summary>
    /// Represents accepted connection from connection listener.
    /// </summary>
    public interface IAcceptedConnection
    {
        /// <summary>
        /// Acquires accepted connection.
        /// </summary>
        /// <returns>Connection context which represents acuired connection.</returns>
        IConnectionContext Acquire();
    }
}
