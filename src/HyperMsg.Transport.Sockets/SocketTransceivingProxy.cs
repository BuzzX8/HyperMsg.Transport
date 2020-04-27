using System;
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

        public int Receive(Memory<byte> buffer) => socket.Receive(buffer.Span);

        public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) => socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).AsTask();

        #endregion
    }
}
