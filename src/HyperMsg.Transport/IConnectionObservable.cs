using System;

namespace HyperMsg.Transport
{
    /// <summary>
    /// Represents registry for connection handlers.
    /// </summary>
    public interface IConnectionObservable
    {
        /// <summary>
        /// Registers handler for accepted connections.
        /// </summary>
        /// <param name="observer"></param>
        void Subscribe(Action<IAcceptedConnection> observer);

        /// <summary>
        /// Registers async handler for accepted connections.
        /// </summary>
        /// <param name="observer"></param>
        void Subscribe(AsyncAction<IAcceptedConnection> observer);
    }
}
