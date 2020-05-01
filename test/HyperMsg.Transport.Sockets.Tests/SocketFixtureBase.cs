using System;
using System.Net;

namespace HyperMsg.Transport.Sockets
{
    public abstract class SocketFixtureBase : IDisposable
    {
        private const int DefaultBufferSize = 2048;
        private readonly IPEndPoint EndPoint;
        private readonly ServiceContainer serviceContainer;

        protected readonly IMessagingContext MessagingContext;
        protected readonly SocketConnectionObservable ConnectionListener;

        public SocketFixtureBase(int port)
        {
            EndPoint = new IPEndPoint(IPAddress.Loopback, port);
            serviceContainer = new ServiceContainer();
            serviceContainer.AddCoreServices(DefaultBufferSize, DefaultBufferSize);
            serviceContainer.AddSocketTransport(EndPoint);
            MessagingContext = serviceContainer.GetRequiredService<IMessagingContext>();
            ConnectionListener = new SocketConnectionObservable(EndPoint);
        }

        protected T GetService<T>() => serviceContainer.GetService<T>();

        public void Dispose()
        {            
            ConnectionListener.Close();
        }
    }
}
