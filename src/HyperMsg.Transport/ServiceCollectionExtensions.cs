using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace HyperMsg.Transport
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBufferTransmitter(this IServiceCollection services, AsyncAction<ReadOnlyMemory<byte>> bufferTransmitter)
        {
            var transmitters = services.SingleOrDefault(s => s.ServiceType == typeof(BufferTransmitters))?.ImplementationInstance as BufferTransmitters;
                       
            if (transmitters == null)
            {
                transmitters = new BufferTransmitters();
                services.AddHostedService<BufferTransmissionService>()
                    .AddSingleton(transmitters);
            }

            transmitters.Add(bufferTransmitter);
            return services;
        }

        public static IServiceCollection AddConnectionCommandService<T>(this IServiceCollection services) where T : ConnectionCommandService => services.AddHostedService<T>();
    }
}
