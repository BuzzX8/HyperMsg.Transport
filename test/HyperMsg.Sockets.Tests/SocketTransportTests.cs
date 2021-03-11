using HyperMsg.Transport;
using HyperMsg.Extensions;
using HyperMsg.Sockets.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Net.Security;
using HyperMsg.Sockets.Properties;
using System.Security.Cryptography.X509Certificates;

namespace HyperMsg.Sockets
{
    public class SocketTransportTests : IDisposable
    {
        private readonly TimeSpan waitTimeout = TimeSpan.FromSeconds(5);
        private readonly int Port = 8888;
        private const int IterationCount = 1000;

        private readonly ServiceHost host;
        private readonly IMessageSender messageSender;
        
        private Socket acceptedSocket;

        private readonly ManualResetEventSlim acceptEvent = new();

        public SocketTransportTests()
        {
            var listeningEndpoint = new IPEndPoint(IPAddress.Loopback, Port);

            host = ServiceHost.CreateDefault(services => services.AddLocalSocketConnection(Port)
                .AddSocketConnectionListener(listeningEndpoint));
            host.StartAsync().Wait();
            messageSender = host.GetRequiredService<IMessageSender>();
            
            var registry = host.GetRequiredService<IMessageHandlersRegistry>();            
            registry.RegisterAcceptedSocketHandler(socket =>
            {
                acceptedSocket = socket;
                acceptEvent.Set();
                return true;
            });
        }

        [Fact]
        public async Task TransmitBufferDataAsync_Transmits_Buffer_Content_With_Socket_Transport()
        {
            var transmittedMessages = new List<Guid>();
            var receivedMessages = new List<Guid>();
            
            await OpenConnectionAsync();
            
            var receiveBuffer = new byte[16];

            for (int i = 0; i < IterationCount; i++)
            {
                var transmittingData = Guid.NewGuid();
                await messageSender.TransmitAsync(transmittingData.ToByteArray(), default);
                transmittedMessages.Add(transmittingData);
                
                acceptedSocket.ReceiveAsync(receiveBuffer, SocketFlags.None).Wait(waitTimeout);
                receivedMessages.Add(new Guid(receiveBuffer));
            }

            Assert.Equal(transmittedMessages, receivedMessages);
        }

        [Fact]
        public async Task Invokes_Buffer_Data_Receiver_When_Receiving_Data_With_Socket_Transport()
        {
            var transmittedMessages = new List<Guid>();
            var receivedMessages = new List<Guid>();
            var receiveEvent = new ManualResetEventSlim();
            var handlersRegistry = host.GetRequiredService<IMessageHandlersRegistry>();
            handlersRegistry.RegisterReceiveHandler<IBuffer>(buffer =>
            {
                var message = new Guid(buffer.Reader.Read().ToArray());
                receivedMessages.Add(message);
                buffer.Clear();
                receiveEvent.Set();
            });

            await OpenConnectionAsync();

            for (int i = 0; i < IterationCount; i++)
            {
                var transmittingData = Guid.NewGuid();
                await acceptedSocket.SendAsync(transmittingData.ToByteArray(), SocketFlags.None);
                transmittedMessages.Add(transmittingData);

                Assert.True(receiveEvent.Wait(1000));                

                receiveEvent.Reset();
            }

            Assert.Equal(transmittedMessages, receivedMessages);
        }

        [Fact]
        public async Task SetTls_()
        {
            await OpenConnectionAsync();

            var sslStream = new SslStream(new NetworkStream(acceptedSocket));
            var sslTask = messageSender.SendAsync(ConnectionCommand.SetTransportLevelSecurity, default);
            var certificate = new X509Certificate(Resources.cert);
            sslStream.AuthenticateAsServer(certificate, false, false);

            Assert.True(sslTask.IsCompletedSuccessfully);
        }

        private async Task OpenConnectionAsync()
        {
            await messageSender.SendAsync(ConnectionListeneningCommand.StartListening, default);
            await messageSender.SendAsync(ConnectionCommand.Open, default);
            acceptEvent.Wait(waitTimeout);

            Assert.True(acceptEvent.IsSet);
            Assert.NotNull(acceptedSocket);
        }

        public void Dispose()
        {
            host.Dispose();
            acceptedSocket?.Dispose();
        }
    }
}
