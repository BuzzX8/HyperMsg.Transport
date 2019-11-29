using HyperMsg.Transport;
using System;
using System.Net;
using System.Net.Sockets;

namespace HyperMsg.Socket
{
    public static class ConfigurableExtensions
    {
        public static void UseSockets(this IConfigurable configurable, EndPoint endpoint)
        {
            var socket = CreateDefaultSocket(endpoint);
            configurable.UseConnection(socket);
            configurable.UseTransmitter(socket);
            configurable.UsePollDataHandler(socket, TimeSpan.FromMilliseconds(200));
        }

        private static SocketProxy CreateDefaultSocket(EndPoint endPoint)
        {            
            var socket = new System.Net.Sockets.Socket(SocketType.Stream, ProtocolType.Tcp);

            return new SocketProxy(socket, endPoint);
        }
    }
}
