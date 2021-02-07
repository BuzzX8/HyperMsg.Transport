using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace HyperMsg.Http
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpTransport(this IServiceCollection services, HttpMessageInvoker messageInvoker)
        {            
            return services.AddSingleton(messageInvoker)
                .AddHostedService<HttpTransportService>();
        }
    }
}
