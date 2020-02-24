using System;

namespace HyperMsg.Transport
{
    /// <summary>
    /// Represents registry for connection handlers.
    /// </summary>
    public interface IConnectionHandlerRegistry
    {
        /// <summary>
        /// Registers handler for accepted connections.
        /// </summary>
        /// <param name="handler"></param>
        void Register(Action<IAcceptedConnection> handler);

        /// <summary>
        /// Registers async handler for accepted connections.
        /// </summary>
        /// <param name="handler"></param>
        void Register(AsyncAction<IAcceptedConnection> handler);
    }
}
