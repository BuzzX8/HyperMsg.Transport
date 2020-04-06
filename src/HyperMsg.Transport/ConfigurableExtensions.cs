using System;

namespace HyperMsg.Transport
{
    public static class ConfigurableExtensions
    {
        /// <summary>
        /// Adds command handler which dispatches transport commands to port. Depends on IMessageSender and IMessageHandlerRegistry.
        /// </summary>
        /// <param name="configurable"></param>
        /// <param name="port"></param>
        public static void AddTransportCommandHandler(this IConfigurable configurable, IPort port)
        {            
            configurable.AddInitializer(provider =>
            {
                var messageSender = provider.GetRequiredService<IMessageSender>();
                var connectionHandler = new ConnectionCommandHandler(port, messageSender);
                var observable = provider.GetRequiredService<IMessageObservable>();
                observable.Subscribe<TransportCommand>(connectionHandler.HandleAsync);
            });
        }

        /// <summary>
        /// Adds handler dispatches flush requests from transmitting buffer to transmitter. Depends on IBufferContext.
        /// </summary>
        /// <param name="configurable"></param>
        /// <param name="transmitter"></param>
        public static void AddDataTransmissionCommandHandler(this IConfigurable configurable, ITransmitter transmitter)
        {            
            configurable.AddInitializer(provider =>
            {
                var dataHandler = new DataTransmissionHandler(transmitter);
                var bufferContext = provider.GetRequiredService<IBufferContext>();
                bufferContext.TransmittingBuffer.FlushRequested += dataHandler.HandleBufferFlushAsync;
            });
        }

        public static void UsePollDataHandler(this IConfigurable configurable, IReceiver receiver, TimeSpan pollInterval)
        {
            configurable.AddInitializer(provider =>
            {
                var bufferContext = provider.GetRequiredService<IBufferContext>();
                var dataHandler = new PollDataHandler(bufferContext.ReceivingBuffer, receiver, pollInterval);
                var observable = provider.GetRequiredService<IMessageObservable>();
                observable.Subscribe<TransportEvent>(dataHandler.Handle);
            });
        }

        public static void UseWorkerDataHandler(this IConfigurable configurable, AsyncAction asyncAction)
        {
            configurable.AddInitializer(provider =>
            {
                var worker = new WorkerDataHandler(asyncAction);
                var observable = provider.GetRequiredService<IMessageObservable>();
                observable.Subscribe<TransportEvent>(worker.HandleTransportEventAsync);
            });
        }
    }
}
