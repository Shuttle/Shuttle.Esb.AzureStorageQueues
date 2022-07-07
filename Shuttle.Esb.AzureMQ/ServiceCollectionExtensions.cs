using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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

            services.AddSingleton<IValidateOptions<ConnectionStringSettings>, ConnectionStringSettingsValidator>();

            services.TryAddSingleton<IAzureMQConfiguration, AzureMQConfiguration>();
            services.TryAddSingleton<IQueueFactory, AzureStorageQueueFactory>();
            
            return services;
        }
    }
}