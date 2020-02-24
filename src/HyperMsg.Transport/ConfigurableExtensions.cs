using System;

namespace HyperMsg.Transport
{
    public static class ConfigurableExtensions
    {
        private static readonly string PortSettingName = typeof(IPort).FullName;
        private static readonly string ReceiverSettingName = typeof(IReceiver).FullName;
        private static readonly string TransmitterSettingName = typeof(ITransmitter).FullName;

        /// <summary>
        /// Registers port which will be used for transport commands. Depends on IMessageSender and IMessageHandlerRegistry.
        /// </summary>
        /// <param name="configurable"></param>
        /// <param name="port"></param>
        public static void UsePort(this IConfigurable configurable, IPort port)
        {
            configurable.AddSetting(PortSettingName, port);
            configurable.RegisterConfigurator((p, s) =>
            {
                var messageSender = p.GetRequiredService<IMessageSender>();
                var connectionHandler = new ConnectionCommandHandler(s.Get<IPort>(PortSettingName), messageSender);
                var handlerRegistry = p.GetRequiredService<IMessageHandlerRegistry>();
                handlerRegistry.Register<TransportCommand>(connectionHandler.HandleAsync);
            });
        }

        /// <summary>
        /// Registers transmitter for handling flush requests from transmitting buffer. Depends on IBufferContext.
        /// </summary>
        /// <param name="configurable"></param>
        /// <param name="transmitter"></param>
        public static void UseTransmitter(this IConfigurable configurable, ITransmitter transmitter)
        {
            configurable.AddSetting(TransmitterSettingName, transmitter);
            configurable.RegisterConfigurator((p, s) =>
            {
                var dataHandler = new DataTransmissionHandler(s.Get<ITransmitter>(TransmitterSettingName));
                var bufferContext = p.GetRequiredService<IBufferContext>();
                bufferContext.TransmittingBuffer.FlushRequested += dataHandler.HandleBufferFlushAsync;
            });
        }

        public static void UsePollDataHandler(this IConfigurable configurable, IReceiver receiver, TimeSpan pollInterval)
        {
            configurable.AddSetting(ReceiverSettingName, receiver);
            configurable.AddSetting(nameof(pollInterval), pollInterval);
            configurable.RegisterConfigurator((p, s) =>
            {
                var bufferContext = p.GetRequiredService<IBufferContext>();
                var dataHandler = new PollDataHandler(bufferContext.ReceivingBuffer, s.Get<IReceiver>(ReceiverSettingName), s.Get<TimeSpan>(nameof(pollInterval)));
                var handlerRegistry = (IMessageHandlerRegistry)p.GetService(typeof(IMessageHandlerRegistry));
                handlerRegistry.Register<TransportEvent>(dataHandler.Handle);
            });
        }

        public static void UseWorkerDataHandler(this IConfigurable configurable, AsyncAction asyncAction)
        {
            configurable.AddSetting(nameof(asyncAction), asyncAction);
            configurable.RegisterConfigurator((p, s) =>
            {
                var worker = new WorkerDataHandler(s.Get<AsyncAction>(nameof(asyncAction)));
                var handlerRegistry = p.GetRequiredService<IMessageHandlerRegistry>();
                handlerRegistry.Register<TransportEvent>(worker.HandleTransportEventAsync);
            });
        }
    }
}
