using System;

namespace HyperMsg.Transport
{
    /// <summary>
    /// Represents context of acuired connection. Must be disposed at the end of lifecycle.
    /// </summary>
    public interface IConnectionContext : IDisposable
    {
        /// <summary>
        /// Buffer context attached to connection.
        /// </summary>
        IBufferContext BufferContext { get; }
    }
}
