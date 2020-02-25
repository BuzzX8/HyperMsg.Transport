using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport.Sockets
{
    /// <summary>
    /// Listener for incoming socket connections.
    /// </summary>
    public sealed class SocketConnectionListener : IPort, IConnectionHandlerRegistry, IDisposable
    {
        private readonly Func<IBufferContext> contextProvider;
        private readonly Socket listeningSocket;
        private readonly SocketAsyncEventArgs eventArgs;
        private readonly EndPoint endPoint;
        private readonly int backlog;

        private AsyncAction<IAcceptedConnection> asyncHandlers;
        private Action<IAcceptedConnection> handlers;

        public SocketConnectionListener(Func<IBufferContext> contextProvider, EndPoint endPoint, int backlog = 1)
        {
            this.contextProvider = contextProvider;
            this.endPoint = endPoint;
            this.backlog = backlog;
            eventArgs = new SocketAsyncEventArgs();
            eventArgs.Completed += OnSocketAccepted;
            listeningSocket = SocketFactory.CreateTcpSocket();
            IsOpen = false;
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            listeningSocket.Bind(endPoint);
            listeningSocket.Listen(backlog);
            IsOpen = true;
            AcceptSocketAsync();
        }

        public Task OpenAsync(CancellationToken cancellationToken)
        {
            Open();
            return Task.CompletedTask;
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }
            
            listeningSocket.Close();
            IsOpen = false;
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            Close();
            return Task.CompletedTask;
        }

        public void Register(Action<IAcceptedConnection> handler) => handlers += handler;

        public void Register(AsyncAction<IAcceptedConnection> handler) => asyncHandlers += handler;

        public void Dispose() => Close();

        private void OnSocketAccepted(object sender, SocketAsyncEventArgs eventArgs)
        {
            HandleAcceptedSocket(eventArgs.AcceptSocket, true);
        }

        private void HandleAcceptedSocket(Socket socket, bool runAccept)
        {
            var acceptedConnection = new AcceptedSocketConnection(contextProvider, socket);

            try
            {
                handlers?.Invoke(acceptedConnection);
                asyncHandlers?.Invoke(acceptedConnection, CancellationToken.None).Wait();
            }
            finally
            {
                if (!acceptedConnection.ConnectionAcquired)
                {
                    acceptedConnection.Dispose();
                }
            }

            if (runAccept)
            {
                AcceptSocketAsync();
            }
        }

        private void AcceptSocketAsync()
        {
            eventArgs.AcceptSocket = null;
            while (!listeningSocket.AcceptAsync(eventArgs))
            {
                HandleAcceptedSocket(eventArgs.AcceptSocket, false);
            }            
        }
    }
}
