using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport.Extensions
{
    public static class MessageSenderExtensions
    {
        public static void SendSetEndpoint<T>(this IMessageSender messageSender, T endpoint) => messageSender.Send(new SetEndpoint<T>(endpoint));

        public static Task SendSetEndpointAsync<T>(this IMessageSender messageSender, T endpoint, CancellationToken cancellationToken = default) => 
            messageSender.SendAsync(new SetEndpoint<T>(endpoint), cancellationToken);
    }
}
