using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureStorageQueues
{
    public class AzureStorageQueueBuilder
    {
        internal readonly Dictionary<string, AzureStorageQueueOptions> AzureStorageQueueOptions = new Dictionary<string, AzureStorageQueueOptions>();
        public IServiceCollection Services { get; }

        public AzureStorageQueueBuilder(IServiceCollection services)
        {
            Guard.AgainstNull(services, nameof(services));
            
            Services = services;
        }

        public AzureStorageQueueBuilder AddOptions(string name, AzureStorageQueueOptions azureStorageQueueOptions)
        {
            Guard.AgainstNullOrEmptyString(name, nameof(name));
            Guard.AgainstNull(azureStorageQueueOptions, nameof(azureStorageQueueOptions));

            AzureStorageQueueOptions.Remove(name);

            AzureStorageQueueOptions.Add(name, azureStorageQueueOptions);

            return this;
        }
    }
}