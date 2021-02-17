using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace HyperMsg.Http.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpTransport(this IServiceCollection services, HttpMessageInvoker messageInvoker)
        {
            return services.AddSingleton(messageInvoker)
                .AddHostedService<HttpTransportService>();
        }

        public static IServiceCollection AddHttpListener(this IServiceCollection services, params Uri[] listeningUris)
        {
            return services.AddHostedService(provider =>
            {
                var messagingContext = provider.GetRequiredService<IMessagingContext>();
                return new HttpListenerService(messagingContext, listeningUris);
            });
        }
    }
}
