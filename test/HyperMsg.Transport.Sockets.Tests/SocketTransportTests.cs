using HyperMsg.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Buffers;
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
        private IConnectionContext acceptedContext;

        private readonly ManualResetEventSlim connectionEvent = new();
        private readonly ManualResetEventSlim transmitEvent = new();
        private readonly ManualResetEventSlim receiveEvent = new();
        
        private byte[] receivedData;

        public SocketTransportTests()
        {
            var services = new ServiceCollection();
            services.AddMessagingServices()
                        .AddSocketTransport("localhost", Port)
                        .AddBufferDataReceiveObserver(buffer =>
                        {
                            receivedData = buffer.Reader.Read().ToArray();
                            receiveEvent.Set();                            
                        })                        
                        .AddConnectionObserver(connection =>
                        {
                            acceptedContext = connection.Acquire();
                            connectionEvent.Set();
                        });

            host = new(services);
            messageSender = host.Services.GetRequiredService<IMessageSender>();
            host.Start();
        }

        [Fact]
        public async Task TransmitBufferDataAsync_Transmits_Buffer_Content_With_Socket_Transport()
        {
            var transmittingData = Guid.NewGuid().ToByteArray();
            await OpenConnectionAsync();

            Assert.NotNull(acceptedContext);
            await TransmittAsync(transmittingData);

            var receivedData = await ReceiveAsync();
            Assert.Equal(transmittingData, receivedData);
        }

        [Fact]
        public async Task Invokes_Buffer_Data_Receiver_When_Receiving_Data_With_Socket_Transport()
        {
            var transmittingData = Guid.NewGuid().ToByteArray();
            await OpenConnectionAsync();

            await acceptedContext.Transmitter.TransmitAsync(transmittingData, default);
            receiveEvent.Wait(waitTimeout);

            Assert.Equal(transmittingData, receivedData);
        }

        private async Task OpenConnectionAsync()
        {
            await messageSender.SendAsync(TransportCommand.Open, default);
            connectionEvent.Wait(waitTimeout);
        }

        private async Task TransmittAsync(byte[] data)
        {
            var bufferContext = host.Services.GetRequiredService<IBufferContext>();
            var buffer = bufferContext.TransmittingBuffer;

            buffer.Writer.Write(data);
            await messageSender.TransmitBufferDataAsync(buffer, default);
            transmitEvent.Set();            
        }

        private async Task<byte[]> ReceiveAsync()
        {
            transmitEvent.Wait(waitTimeout);
            var bufferContext = host.Services.GetRequiredService<IBufferContext>();
            var buffer = bufferContext.ReceivingBuffer;

            await acceptedContext.Receiver.ReceiveAsync(buffer.Writer, default);
            return buffer.Reader.Read().ToArray();
        }

        public void Dispose() => host.Dispose();
    }
}
