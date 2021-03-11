using HyperMsg.Extensions;
using HyperMsg.Transport;
using HyperMsg.Transport.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HyperMsg.Sockets
{
    internal class SocketConnectionListeneningService : MessagingService
    {
        private readonly Func<Socket> socketFactory;
        private EndPoint listeningEndpoint;
        private readonly int backlog;
                
        private Socket listeningSocket;
        private Task<Socket> currentAcceptTask;

        public SocketConnectionListeneningService(IMessagingContext messagingContext, Func<Socket> socketFactory, EndPoint listeningEndpoint, int backlog) : base(messagingContext)
        {
            this.socketFactory = socketFactory;
            this.listeningEndpoint = listeningEndpoint;
            this.backlog = backlog;
        }

        protected override IEnumerable<IDisposable> GetDefaultDisposables()
        {
            yield return this.RegisterHandler(ConnectionListeneningCommand.StartListening, StartListening);
            yield return this.RegisterHandler(ConnectionListeneningCommand.StopListening, StopListening);
            yield return this.RegisterSetEndpointHandler<EndPoint>(SetEndpoint);
            yield return this.RegisterSetEndpointHandler<IPEndPoint>(SetEndpoint);
        }

        private void SetEndpoint(EndPoint listeningEndpoint) => this.listeningEndpoint = listeningEndpoint;

        private void StartListening()
        {
            if (listeningSocket != null)
            {
                return;
            }

            listeningSocket = socketFactory.Invoke();
            listeningSocket.Bind(listeningEndpoint ?? throw new TransportException("Listening endpoint was not provided."));
            listeningSocket.Listen(backlog);
            AcceptSocket();            
        }

        private void AcceptSocket()
        {
            currentAcceptTask = listeningSocket.AcceptAsync();
            currentAcceptTask.GetAwaiter().OnCompleted(() =>
            {
                if (!listeningSocket.Connected && !currentAcceptTask.IsCompletedSuccessfully)
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

                AcceptSocket();
            });            
        }

        private void StopListening()
        {            
            if (listeningSocket == null)
            {
                return;
            }
                        
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
