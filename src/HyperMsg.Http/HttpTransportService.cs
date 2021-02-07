using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Http
{
    internal class HttpTransportService : MessagingObject, IHostedService
    {
        private readonly HttpMessageInvoker messageInvoker;

        public HttpTransportService(HttpMessageInvoker messageInvoker, IMessagingContext messagingContext) : base(messagingContext)
        {
            this.messageInvoker = messageInvoker;

            RegisterHandler<HttpRequestMessage>(HandleAsync);
        }

        private Task HandleAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
        {
            var task = messageInvoker.SendAsync(httpRequest, cancellationToken);
            task.GetAwaiter().OnCompleted(() => OnRequestTaskCompleted(task));

            return Task.CompletedTask;
        }

        private void OnRequestTaskCompleted(Task<HttpResponseMessage> response)
        {
            if (response.Status == TaskStatus.Faulted)
            {
                response.Exception.Flatten();

                foreach(var exception in response.Exception.InnerExceptions)
                {
                    Send(exception);
                }

                return;
            }

            Receive(response.Result);
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override void Dispose()
        {
            base.Dispose();
            messageInvoker.Dispose();
        }        
    }    
}
