using System;
using System.Buffers;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace HyperMsg.Transport
{
    public class PollDataHandler : IDisposable
    {
        private readonly IBuffer buffer;
        private readonly IReceiver receiver;
        private readonly Timer timer;

        private readonly object sync = new object();

        public PollDataHandler(IBuffer buffer, IReceiver receiver, TimeSpan interval)
        {
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            this.receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            timer = new Timer(interval.TotalMilliseconds);
            timer.Elapsed += Timer_Elapsed;
        }

        private IBufferWriter<byte> Writer => buffer.Writer;

        public void Handle(TransportEvent transportEvent)
        {
            switch (transportEvent)
            {
                case TransportEvent.Opened:
                    timer.Start();
                    break;

                case TransportEvent.Closed:
                    timer.Stop();
                    break;
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Monitor.IsEntered(sync))
            {
                return;
            }

            lock (sync)
            {
                var memory = Writer.GetMemory();
                var bytesReceived = receiver.Receive(memory);
                Writer.Advance(bytesReceived);
                buffer.FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        public void Dispose()
        {
            timer.Elapsed -= Timer_Elapsed;
            timer.Dispose();
        }
    }
}
