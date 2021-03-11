using HyperMsg.Transport;
using HyperMsg.Transport.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Sockets.Extensions
{
    public static class MessageSenderExtensions
    {
        public static void SendSetIPEndpoint(this IMessageSender messageSender, string hostNameOrAddress, int port, Func<IReadOnlyList<IPAddress>, IPAddress> addressSelector = null)
        {
            IPAddress[] addresses;

            try
            {
                addresses = Dns.GetHostAddresses(hostNameOrAddress);
            }
            catch(SocketException e)
            {
                throw new TransportException(e.Message, e);
            }

            var address = addressSelector?.Invoke(addresses) ?? addresses[0];

            messageSender.SendSetEndpoint(new IPEndPoint(address, port));
        }

        public static async Task SendSetIPEndpointAsync(this IMessageSender messageSender, string hostNameOrAddress, int port, Func<IReadOnlyList<IPAddress>, IPAddress> addressSelector = null, CancellationToken cancellationToken = default)
        {
            IPAddress[] addresses;

            try
            {
                addresses = await Dns.GetHostAddressesAsync(hostNameOrAddress);
            }
            catch (ArgumentException e)
            {
                throw new TransportException(e.Message, e);
            }
            catch(SocketException e)
            {
                throw new TransportException(e.Message, e);
            }

            var address = addressSelector?.Invoke(addresses) ?? addresses[0];

            await messageSender.SendSetEndpointAsync(new IPEndPoint(address, port), cancellationToken);
        }
    }
}
