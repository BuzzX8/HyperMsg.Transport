using HyperMsg.Extensions;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Http
{
    internal class HttpTransportService : MessagingService
    {
        private readonly HttpMessageInvoker messageInvoker;

        public HttpTransportService(HttpMessageInvoker messageInvoker, IMessagingContext messagingContext) : base(messagingContext)
        {
            this.messageInvoker = messageInvoker;
        }

        protected override IEnumerable<System.IDisposable> GetDefaultDisposables()
        {
            yield return RegisterHandler<HttpRequestMessage>(HandleAsync);
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

            this.Receive(response.Result);
        }        

        public override void Dispose()
        {
            base.Dispose();
            messageInvoker.Dispose();
        }        
    }    
}
