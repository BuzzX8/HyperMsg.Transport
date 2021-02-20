using HyperMsg.Transport;
using HyperMsg.Extensions;
using HyperMsg.Http.Extensions;
using HyperMsg.WebSockets.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HyperMsg.WebSockets
{
    public class WebSocketTransportTests : IDisposable
    {
        private readonly TimeSpan waitTimeout = TimeSpan.FromSeconds(5);
        private readonly Uri uri = new Uri("ws://localhost:9090/");

        private readonly ServiceHost host;
        private readonly IMessageSender messageSender;

        private HttpListenerWebSocketContext webSocketContext;
        private WebSocket acceptedSocket;

        private readonly ManualResetEventSlim acceptEvent = new();

        public WebSocketTransportTests()
        {
            host = ServiceHost.CreateDefault(services =>
            {
                services.AddWebSocketConnection(options => options.AddSubProtocol("test-proto"), uri)
                    .AddHttpListener(new Uri("http://localhost:9090"));
            });
            messageSender = host.GetRequiredService<IMessageSender>();
            var handlersRegistry = host.GetRequiredService<IMessageHandlersRegistry>();
            handlersRegistry.RegisterHandler<HttpListenerContext>(async (context, token) =>
            {
                webSocketContext = await context.AcceptWebSocketAsync("test-proto");
                acceptedSocket = webSocketContext.WebSocket;
                acceptEvent.Set();
            });
            host.StartAsync().Wait();
        }

        [Fact]
        public async Task TransmitBufferDataAsync_Transmits_Buffer_Content_With_Socket_Transport()
        {
            var transmittedMessages = new List<Guid>();
            var receivedMessages = new List<Guid>();

            await OpenConnectionAsync();

            var receiveBuffer = new byte[16];

            for (int i = 0; i < 10; i++)
            {
                var transmittingData = Guid.NewGuid();
                await messageSender.TransmitAsync(transmittingData.ToByteArray(), default);
                transmittedMessages.Add(transmittingData);

                acceptedSocket.ReceiveAsync(receiveBuffer, default).Wait(waitTimeout);
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

            for (int i = 0; i < 10; i++)
            {
                var transmittingData = Guid.NewGuid();
                await acceptedSocket.SendAsync(transmittingData.ToByteArray(), WebSocketMessageType.Binary, false, default);
                transmittedMessages.Add(transmittingData);
                receiveEvent.Wait(500);
                receiveEvent.Reset();
            }

            Assert.Equal(transmittedMessages, receivedMessages);
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
            messageSender.Send(ConnectionCommand.Close);
            messageSender.Send(ConnectionListeneningCommand.StopListening);
            host.Dispose();
            acceptedSocket?.Dispose();
        }
    }
}
