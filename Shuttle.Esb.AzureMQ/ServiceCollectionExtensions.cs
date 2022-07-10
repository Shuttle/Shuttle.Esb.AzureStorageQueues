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
        public static IServiceCollection AddAzureStorageQueues(this IServiceCollection services,
            Action<AzureMQBuilder> builder = null)
        {
            Guard.AgainstNull(services, nameof(services));

            var configurationBuilder = new AzureMQBuilder(services);

            builder?.Invoke(configurationBuilder);

            services.AddSingleton<IValidateOptions<ConnectionStringOptions>, ConnectionStringOptionsValidator>();

            services.TryAddSingleton<IQueueFactory, AzureStorageQueueFactory>();
            
            return services;
        }
    }
}