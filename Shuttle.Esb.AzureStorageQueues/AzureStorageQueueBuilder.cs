using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureStorageQueues;

public class AzureStorageQueueBuilder
{
    internal readonly Dictionary<string, AzureStorageQueueOptions> AzureStorageQueueOptions = new();

    public AzureStorageQueueBuilder(IServiceCollection services)
    {
        Services = Guard.AgainstNull(services);
    }

    public IServiceCollection Services { get; }

    public AzureStorageQueueBuilder AddOptions(string name, AzureStorageQueueOptions azureStorageQueueOptions)
    {
        Guard.AgainstNullOrEmptyString(name);
        Guard.AgainstNull(azureStorageQueueOptions);

        AzureStorageQueueOptions.Remove(name);

        AzureStorageQueueOptions.Add(name, azureStorageQueueOptions);

        return this;
    }
}