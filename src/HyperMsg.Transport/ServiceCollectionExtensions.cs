using HyperMsg.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace HyperMsg.Transport
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds command handler which dispatches transport commands to port. Depends on IMessageSender and IMessageHandlerRegistry.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddTransportCommandObserver(this IServiceCollection services) => services.AddObserver<ConnectionCommandComponent, TransportCommand>(component => component.HandleAsync);

        /// <summary>
        /// Adds handler dispatches flush requests from transmitting buffer to transmitter. Depends on IBufferContext.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="transmitter"></param>
        public static IServiceCollection AddBufferDataTransmitObserver(this IServiceCollection services) => services.AddBufferDataTransmitObserver<BufferDataTransmissionComponent>(component => component.HandleAsync);
    }
}
