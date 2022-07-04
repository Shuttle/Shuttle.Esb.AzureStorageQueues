using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureMQ
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAzureMQ(this IServiceCollection services,
            Action<AzureMQConfigurationBuilder> builder = null)
        {
            Guard.AgainstNull(services, nameof(services));

            var configurationBuilder = new AzureMQConfigurationBuilder(services);

            builder?.Invoke(configurationBuilder);

            var azureMQConfigurationType = typeof(IAzureMQConfiguration);

            if (services.All(item=> item.ServiceType != azureMQConfigurationType))
            {
                services.AddSingleton(configurationBuilder.GetConfiguration());
            }

            services.AddSingleton<IQueueFactory, AzureStorageQueueFactory>();

            return services;
        }
    }
}