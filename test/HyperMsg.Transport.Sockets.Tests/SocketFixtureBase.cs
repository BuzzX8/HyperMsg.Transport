using System;
using System.Net;

namespace HyperMsg.Transport.Sockets
{
    public abstract class SocketFixtureBase : IDisposable
    {
        private const int DefaultBufferSize = 2048;
        private readonly IPEndPoint EndPoint;
        private readonly ServiceProvider serviceProvider;

        protected readonly IMessagingContext MessagingContext;
        protected readonly SocketConnectionObservable ConnectionListener;

        public SocketFixtureBase(int port)
        {
            EndPoint = new IPEndPoint(IPAddress.Loopback, port);
            serviceProvider = new ServiceProvider();
            serviceProvider.AddCoreServices(DefaultBufferSize, DefaultBufferSize);
            serviceProvider.AddSocketTransport(EndPoint);
            MessagingContext = serviceProvider.GetRequiredService<IMessagingContext>();
            ConnectionListener = new SocketConnectionObservable(EndPoint);
        }

        protected T GetService<T>() => serviceProvider.GetService<T>();

        public void Dispose()
        {            
            ConnectionListener.Close();
        }
    }
}
