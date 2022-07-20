using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureStorageQueues
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAzureStorageQueues(this IServiceCollection services,
            Action<AzureStorageQueuesBuilder> builder = null)
        {
            Guard.AgainstNull(services, nameof(services));

            var azureStorageQueuesBuilder = new AzureStorageQueuesBuilder(services);

            builder?.Invoke(azureStorageQueuesBuilder);

            services.AddSingleton<IValidateOptions<ConnectionStringOptions>, ConnectionStringOptionsValidator>();

            services.TryAddSingleton<IQueueFactory, AzureStorageQueueFactory>();
            
            return services;
        }
    }
}