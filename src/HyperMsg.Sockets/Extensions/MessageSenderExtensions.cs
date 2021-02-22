using HyperMsg.Transport.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Sockets.Extensions
{
    public static class MessageSenderExtensions
    {
        public static void SendSetIPEndpoint(this IMessageSender messageSender, string hostNameOrAddress, int port, Func<IReadOnlyList<IPAddress>, IPAddress> addressSelector = null)
        {
            var addresses = Dns.GetHostAddresses(hostNameOrAddress);
            var address = addressSelector?.Invoke(addresses) ?? addresses[0];

            messageSender.SendSetEndpoint(new IPEndPoint(address, port));
        }

        public static async Task SendSetIPEndpointAsync(this IMessageSender messageSender, string hostNameOrAddress, int port, Func<IReadOnlyList<IPAddress>, IPAddress> addressSelector = null, CancellationToken cancellationToken = default)
        {
            var addresses = await Dns.GetHostAddressesAsync(hostNameOrAddress);
            var address = addressSelector?.Invoke(addresses) ?? addresses[0];

            await messageSender.SendSetEndpointAsync(new IPEndPoint(address, port), cancellationToken);
        }
    }
}
