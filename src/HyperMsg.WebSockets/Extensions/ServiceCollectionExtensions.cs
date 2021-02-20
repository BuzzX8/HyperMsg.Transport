using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.WebSockets;

namespace HyperMsg.WebSockets.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebSocketConnection(this IServiceCollection services, Action<ClientWebSocketOptions> webSocketConfigurator, Uri uri = null)
        {
            return services.AddSingleton<ClientWebSocket>()
                .AddHostedService(provider =>
                {
                    var messagingContext = provider.GetRequiredService<IMessagingContext>();
                    var webSocket = provider.GetRequiredService<ClientWebSocket>();
                    webSocketConfigurator.Invoke(webSocket.Options);
                    return new WebSocketConnectionService(webSocket, uri, messagingContext);
                })
                .AddHostedService(provider =>
                {
                    var clientWebSocket = provider.GetRequiredService<ClientWebSocket>();
                    var messagingContext = provider.GetRequiredService<IMessagingContext>();
                    var bufferContext = provider.GetRequiredService<IBufferContext>();

                    return new WebSocketDataTransferService(clientWebSocket, bufferContext.ReceivingBuffer, messagingContext);
                });
        }
    }
}
