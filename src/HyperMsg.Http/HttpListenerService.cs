using HyperMsg.Extensions;
using HyperMsg.Transport;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace HyperMsg.Http
{
    public class HttpListenerService : MessagingService
    {
        private readonly HttpListener httpListener;
        private Task<HttpListenerContext> currentListeningTask;

        public HttpListenerService(IMessagingContext messagingContext, Uri[] listeningUris) : base(messagingContext)
        {
            httpListener = new HttpListener();

            foreach(var uri in listeningUris)
            {
                httpListener.Prefixes.Add(uri.ToString());
            }
        }

        protected override IEnumerable<IDisposable> GetDefaultDisposables()
        {
            yield return this.RegisterHandler(ConnectionListeneningCommand.StartListening, StartListening);
            yield return this.RegisterHandler(ConnectionListeneningCommand.StopListening, StopListening);
        }

        private void StartListening()
        {
            httpListener.Start();
            currentListeningTask = httpListener.GetContextAsync();
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
