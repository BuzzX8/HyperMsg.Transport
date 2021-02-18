using HyperMsg.Transport;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Sockets
{
    internal class SocketConnectionListeneningService : MessagingObject, IHostedService
    {
        private readonly Func<Socket> socketFactory;
        private readonly EndPoint listeningEndpoint;
        private readonly int backlog;
                
        private Socket listeningSocket;
        private CancellationTokenSource tokenSource;
        private Task<Socket> currentAcceptTask;

        public SocketConnectionListeneningService(IMessagingContext messagingContext, Func<Socket> socketFactory, EndPoint listeningEndpoint, int backlog) : base(messagingContext)
        {
            this.socketFactory = socketFactory;
            this.listeningEndpoint = listeningEndpoint;
            this.backlog = backlog;

            RegisterHandler(ConnectionListeneningCommand.StartListening, StartListening);
            RegisterHandler(ConnectionListeneningCommand.StopListening, StopListening);
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private void StartListening()
        {
            if (listeningSocket != null)
            {
                return;
            }

            listeningSocket = socketFactory.Invoke();
            listeningSocket.Bind(listeningEndpoint);
            listeningSocket.Listen(backlog);
            tokenSource = new();
            AcceptSocket();            
        }

        private void AcceptSocket()
        {
            currentAcceptTask = listeningSocket.AcceptAsync();
            currentAcceptTask.GetAwaiter().OnCompleted(() =>
            {
                if (tokenSource.IsCancellationRequested)
                {
                    return;
                }

                var acceptedSocket = new AcceptedSocket { Socket = currentAcceptTask.Result };

                try
                {
                    Send(acceptedSocket);
                }
                finally
                {
                    if (!acceptedSocket.IsAquired)
                    {
                        acceptedSocket.Socket.Dispose();
                    }
                }
            });
        }

        private void StopListening()
        {            
            if (listeningSocket == null)
            {
                return;
            }

            tokenSource.Cancel();
            listeningSocket.Dispose();
            listeningSocket = null;
        }

        public override void Dispose()
        {
            base.Dispose();
            listeningSocket?.Dispose();
        }
    }

    internal class AcceptedSocket
    {
        internal Socket Socket { get; set; }

        internal bool IsAquired { get; set; }
    }
}
