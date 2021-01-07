using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMsg.Transport.Sockets
{
    public class SocketTransceivingProxy : ITransmitter, IReceiver
    {
        private readonly Socket socket;

        public SocketTransceivingProxy(Socket socket)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        #region ITransmitter

        public void Transmit(ReadOnlyMemory<byte> data) => socket.Send(data.Span);

        public Task TransmitAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) => socket.SendAsync(data, SocketFlags.None, cancellationToken).AsTask();

        #endregion

        #region IReceiver

        public int Receive(IBufferWriter<byte> bufferWriter)
        {
            var buffer = bufferWriter.GetSpan();
            var bytesReceived = socket.Receive(buffer);
            bufferWriter.Advance(bytesReceived);
            return bytesReceived;
        }

        public async Task<int> ReceiveAsync(IBufferWriter<byte> bufferWriter, CancellationToken cancellationToken)
        {
            var buffer = bufferWriter.GetMemory();
            var bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).AsTask();
            bufferWriter.Advance(bytesReceived);
            return bytesReceived;
        }

        #endregion
    }
}
