using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureStorageQueues
{
    public class AzureStorageQueuesBuilder
    {
        internal readonly Dictionary<string, AzureStorageQueueOptions> AzureStorageQueueOptions = new Dictionary<string, AzureStorageQueueOptions>();
        public IServiceCollection Services { get; }

        public AzureStorageQueuesBuilder(IServiceCollection services)
        {
            Guard.AgainstNull(services, nameof(services));
            
            Services = services;
        }

        public AzureStorageQueuesBuilder AddOptions(string name, AzureStorageQueueOptions amazonSqsOptions)
        {
            Guard.AgainstNullOrEmptyString(name, nameof(name));
            Guard.AgainstNull(amazonSqsOptions, nameof(amazonSqsOptions));

            AzureStorageQueueOptions.Remove(name);

            AzureStorageQueueOptions.Add(name, amazonSqsOptions);

            return this;
        }
    }
}