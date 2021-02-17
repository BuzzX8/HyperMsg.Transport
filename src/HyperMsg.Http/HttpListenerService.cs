using HyperMsg.Connection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Http
{
    public class HttpListenerService : MessagingObject, IHostedService
    {
        private readonly HttpListener httpListener;
        private Task<HttpListenerContext> currentListeningTask;

        public HttpListenerService(IMessagingContext messagingContext, Uri[] listeningUris) : base(messagingContext)
        {
            httpListener = new HttpListener();
            RegisterHandler(ConnectionListeneningCommand.StartListening, StartListening);
            RegisterHandler(ConnectionListeneningCommand.StopListening, StopListening);

            foreach(var uri in listeningUris)
            {
                httpListener.Prefixes.Add(uri.ToString());
            }
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private void StartListening()
        {
            httpListener.Start();
            var currentListeningTask = httpListener.GetContextAsync();
            currentListeningTask.GetAwaiter().OnCompleted(() =>
            {
                var context = currentListeningTask.Result;
                Send(context);
            });
        }

        private void StopListening()
        {
            httpListener.Stop();
        }
    }
}
