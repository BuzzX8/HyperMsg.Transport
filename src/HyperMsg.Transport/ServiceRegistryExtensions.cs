using System;

namespace HyperMsg.Transport
{
    public static class ServiceRegistryExtensions
    {
        /// <summary>
        /// Adds command handler which dispatches transport commands to port. Depends on IMessageSender and IMessageHandlerRegistry.
        /// </summary>
        /// <param name="serviceRegistry"></param>
        /// <param name="port"></param>
        public static void AddTransportCommandHandler(this IServiceRegistry serviceRegistry, IPort port)
        {            
            serviceRegistry.AddService(provider =>
            {
                var context = provider.GetRequiredService<IMessagingContext>();
                return new ConnectionCommandHandler(context, port);
            });
        }

        /// <summary>
        /// Adds handler dispatches flush requests from transmitting buffer to transmitter. Depends on IBufferContext.
        /// </summary>
        /// <param name="serviceRegistry"></param>
        /// <param name="transmitter"></param>
        public static void AddDataTransmissionCommandHandler(this IServiceRegistry serviceRegistry, ITransmitter transmitter)
        {            
            serviceRegistry.AddService(provider =>
            {
                var observable = provider.GetRequiredService<IMessageObservable>();
                return new DataTransmissionHandler(observable, transmitter);
            });
        }

        public static void UsePollDataHandler(this IServiceRegistry serviceRegistry, IReceiver receiver, TimeSpan pollInterval)
        {
            serviceRegistry.AddService(provider =>
            {
                var bufferContext = provider.GetRequiredService<IBufferContext>();
                var dataHandler = new PollDataHandler(bufferContext.ReceivingBuffer, receiver, pollInterval);
                var observable = provider.GetRequiredService<IMessageObservable>();
                observable.Subscribe<TransportEvent>(dataHandler.Handle);
                return dataHandler;
            });
        }

        public static void UseWorkerDataHandler(this IServiceRegistry serviceRegistry, AsyncAction asyncAction)
        {
            serviceRegistry.AddService(provider =>
            {
                var worker = new WorkerDataHandler(asyncAction);
                var observable = provider.GetRequiredService<IMessageObservable>();
                observable.Subscribe<TransportEvent>(worker.HandleTransportEventAsync);
                return worker;
            });
        }
    }
}
