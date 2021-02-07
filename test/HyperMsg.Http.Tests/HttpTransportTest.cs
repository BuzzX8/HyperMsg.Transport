using HyperMsg.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HyperMsg.Http
{
    public class HttpTransportTest
    {
        private readonly ServiceHost host;
        private readonly IMessagingContext messagingContext;
        private readonly TestMessageHandler messageHandler;
        private readonly ManualResetEventSlim receiveEvent;
        private readonly TimeSpan waitTimeout;

        public HttpTransportTest()
        {
            messageHandler = new();
            receiveEvent = new();
            waitTimeout = TimeSpan.FromSeconds(5);
            host = ServiceHost.CreateDefault(services => services.AddHttpTransport(new (messageHandler)));
            host.StartAsync().Wait();
            messagingContext = host.GetRequiredService<IMessagingContext>();
        }

        [Fact]
        public async Task Receives_Response_For_Correct_Request()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.hostname.com");
            var response = default(HttpResponseMessage);
            messagingContext.HandlersRegistry.RegisterReceiveHandler<HttpResponseMessage>(r =>
            {
                response = r;
                receiveEvent.Set();
            });

            await messagingContext.Sender.TransmitAsync(request, default);
            messageHandler.SetResponse(new HttpResponseMessage());
            receiveEvent.Wait(waitTimeout);

            Assert.NotNull(response);
        }

        [Fact]
        public async Task Emits_Exception_For_Incorrect_Request()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://{Guid.NewGuid()}.com");
            var exception = default(HttpRequestException);
            messagingContext.HandlersRegistry.RegisterReceiveHandler<HttpRequestException>(ex =>
            {
                exception = ex;
                receiveEvent.Set();
            });

            await messagingContext.Sender.TransmitAsync(request, default);
            messageHandler.SetException(new HttpRequestException());
            receiveEvent.Wait(waitTimeout);

            Assert.NotNull(exception);
        }
    }

    public class TestMessageHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource<HttpResponseMessage> responseTask = new TaskCompletionSource<HttpResponseMessage>();

        public HttpRequestMessage Request { get; private set; }

        public void SetResponse(HttpResponseMessage response)
        {
            responseTask.SetResult(response);
        }

        public void SetException(Exception exception)
        {
            responseTask.SetException(exception);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return responseTask.Task;
        }
    }
}
