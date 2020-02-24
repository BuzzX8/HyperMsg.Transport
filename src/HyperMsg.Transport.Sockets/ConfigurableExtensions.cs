using System.Net;

namespace HyperMsg.Transport.Sockets
{
    public static class ConfigurableExtensions
    {
        private static readonly string EndPointSettingName = typeof(EndPoint).FullName;

        public static void UseClientSocket(this IConfigurable configurable, EndPoint endpoint)
        {
            configurable.AddSetting(EndPointSettingName, endpoint);
            configurable.RegisterConfigurator((p, s) =>
            {
                var context = p.GetRequiredService<IBufferContext>();
                var socket = CreateDefaultSocket(s.Get<EndPoint>(EndPointSettingName));
                var observer = new SocketDataObserver(context.ReceivingBuffer, socket.InnerSocket);
                socket.Connected += observer.Run;
                configurable.UsePort(socket);
                configurable.UseTransmitter(socket);
            });
        }

        private static SocketProxy CreateDefaultSocket(EndPoint endPoint)
        {
            var socket = SocketFactory.CreateTcpSocket();

            return new SocketProxy(socket, endPoint);
        }
    }
}