using System;

namespace HyperMsg.Transport
{
    public static class ConfigurableExtensions
    {
        public static void UseConnection(this IConfigurable configurable, IConnection connection)
        {
            configurable.AddSetting(nameof(connection), connection);
            configurable.RegisterConfigurator((p, s) =>
            {
                var conn = (IConnection)s[nameof(connection)];
                var messageSender = (IMessageSender)p.GetService(typeof(IMessageSender));
                var connectionHandler = new ConnectionCommandHandler(conn, messageSender);
                var handlerRegistry = (IMessageHandlerRegistry)p.GetService(typeof(IMessageHandlerRegistry));
                handlerRegistry.Register<TransportCommand>(connectionHandler.HandleAsync);
            });
        }

        public static void UseTransmitter(this IConfigurable configurable, ITransmitter transmitter)
        {
            configurable.AddSetting(nameof(transmitter), transmitter);
            configurable.RegisterConfigurator((p, s) =>
            {
                var dataHandler = new DataTransmissionHandler((ITransmitter)s[nameof(transmitter)]);
                var transmittingBuffer = (ITransmittingBuffer)p.GetService(typeof(ITransmittingBuffer));
                transmittingBuffer.FlushRequested += dataHandler.HandleBufferFlushAsync;
            });
        }

        public static void UsePollDataHandler(this IConfigurable configurable, IReceiver receiver, TimeSpan pollInterval)
        {
            configurable.AddSetting(nameof(receiver), receiver);
            configurable.AddSetting(nameof(pollInterval), pollInterval);
            configurable.RegisterConfigurator((p, s) =>
            {
                var receivingBuffer = (IReceivingBuffer)p.GetService(typeof(IReceivingBuffer));                
                var dataHandler = new PollDataHandler(receivingBuffer, (IReceiver)s[nameof(receiver)], (TimeSpan)s[nameof(pollInterval)]);
                var handlerRegistry = (IMessageHandlerRegistry)p.GetService(typeof(IMessageHandlerRegistry));
                handlerRegistry.Register<TransportEvent>(dataHandler.Handle);
            });
        }

        public static void UseWorkerDataHandler(this IConfigurable configurable, AsyncAction asyncAction)
        {
            configurable.AddSetting(nameof(asyncAction), asyncAction);
            configurable.RegisterConfigurator((p, s) =>
            {
                var worker = new WorkerDataHandler((AsyncAction)s[nameof(asyncAction)]);                
                var handlerRegistry = (IMessageHandlerRegistry)p.GetService(typeof(IMessageHandlerRegistry));
                handlerRegistry.Register<TransportEvent>(worker.HandleTransportEventAsync);
            });
        }
    }
}
