using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.WebSockets;

namespace HyperMsg.WebSockets.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebSocketConnection(this IServiceCollection services, Uri uri, Action<ClientWebSocketOptions> webSocketConfigurator)
        {
            return services.AddSingleton(provider =>
            {
                var messagingContext = provider.GetRequiredService<IMessagingContext>();
                var webSocket = new ClientWebSocket();
                webSocketConfigurator.Invoke(webSocket.Options);
                return new WebSocketConnectionService(webSocket, uri, messagingContext);
            }).AddHostedService<WebSocketDataTransferService>();
        }
    }
}
