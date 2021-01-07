using Microsoft.Extensions.Hosting;
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
    internal sealed class SocketConnectionObservable : IHostedService, IPort, IDisposable
    {
        private readonly Socket listeningSocket;        
        private readonly EndPoint endPoint;
        private readonly int backlog;

        private readonly AsyncAction<IAcceptedConnection> asyncHandlers;
        private readonly Action<IAcceptedConnection> handlers;
        private readonly object disposeSync = new object();

        private SocketAsyncEventArgs eventArgs;        

        public SocketConnectionObservable(IServiceProvider serviceProvider, EndPoint endPoint)
        {
            asyncHandlers = serviceProvider.GetService(typeof(AsyncAction<IAcceptedConnection>)) as AsyncAction<IAcceptedConnection>;
            handlers = serviceProvider.GetService(typeof(Action<IAcceptedConnection>)) as Action<IAcceptedConnection>;
            this.endPoint = endPoint;
            backlog = 1;

            
            listeningSocket = SocketFactory.CreateTcpSocket();
            IsOpen = false;
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            eventArgs = new SocketAsyncEventArgs();
            eventArgs.Completed += OnSocketAccepted;
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

            lock (disposeSync)
            {
                eventArgs.Dispose();
                listeningSocket.Close();
                IsOpen = false;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            Close();
            return Task.CompletedTask;
        }

        public void Dispose() => Close();

        private void OnSocketAccepted(object sender, SocketAsyncEventArgs eventArgs) => HandleAcceptedSocket(eventArgs.AcceptSocket, true);

        private void HandleAcceptedSocket(Socket socket, bool runAccept)
        {
            lock (disposeSync)
            {
                if (!IsOpen)
                {
                    return;
                }
            }

            var acceptedConnection = new AcceptedSocketConnection(socket);

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

        public Task StartAsync(CancellationToken cancellationToken) => OpenAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Close();
            return Task.CompletedTask;
        }
    }
}
