using HyperMsg.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HyperMsg.Transport.Sockets
{
    public class SocketTransportTests : IDisposable
    {
        private readonly TimeSpan waitTimeout = TimeSpan.FromSeconds(5);
        private readonly int Port = 8888;

        private readonly Host host;
        private readonly IMessageSender messageSender;

        private readonly Socket listeningSocket;
        private Socket acceptedSocket;

        private readonly ManualResetEventSlim transmitEvent = new();
        private readonly ManualResetEventSlim receiveEvent = new();
        
        private byte[] receivedData;

        public SocketTransportTests()
        {
            listeningSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listeningSocket.Bind(new IPEndPoint(IPAddress.Loopback, Port));

            host = Host.CreateDefault(services => services.AddLocalIPEndPoint(Port).AddSocketServices());
            host.StartAsync().Wait();
            messageSender = host.GetRequiredService<IMessageSender>();
            var observable = host.GetRequiredService<IMessageObservable>();
            observable.OnReceived<IBuffer>(buffer =>
            {
                receivedData = buffer.Reader.Read().ToArray();
                receiveEvent.Set();
            });
        }

        [Fact]
        public async Task TransmitBufferDataAsync_Transmits_Buffer_Content_With_Socket_Transport()
        {
            var transmittingData = Guid.NewGuid().ToByteArray();
            await OpenConnectionAsync();

            Assert.NotNull(acceptedSocket);
            await TransmittAsync(transmittingData);

            var actualData = new byte[transmittingData.Length];
            await acceptedSocket.ReceiveAsync(actualData, SocketFlags.None);

            Assert.Equal(transmittingData, actualData);
        }

        [Fact]
        public async Task Invokes_Buffer_Data_Receiver_When_Receiving_Data_With_Socket_Transport()
        {
            var transmittingData = Guid.NewGuid().ToByteArray();
            await OpenConnectionAsync();

            acceptedSocket.Send(transmittingData);
            receiveEvent.Wait(waitTimeout);

            Assert.Equal(transmittingData, receivedData);
        }

        private async Task OpenConnectionAsync()
        {
            listeningSocket.Listen();
            
            var acceptTask = listeningSocket.AcceptAsync();
            await messageSender.SendAsync(TransportCommand.Open, default);
            acceptTask.Wait(waitTimeout);
            Assert.True(acceptTask.IsCompleted);

            acceptedSocket = acceptTask.Result;
        }

        private async Task TransmittAsync(byte[] data)
        {
            var bufferContext = host.Services.GetRequiredService<IBufferContext>();
            var buffer = bufferContext.TransmittingBuffer;

            buffer.Writer.Write(data);
            await messageSender.TransmitAsync(buffer, default);
            transmitEvent.Set();            
        }

        public void Dispose()
        {
            host.Dispose();
            listeningSocket.Dispose();
            acceptedSocket?.Dispose();
        }
    }
}
