using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureStorageQueues;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureStorageQueues(this IServiceCollection services, Action<AzureStorageQueueBuilder>? builder = null)
    {
        Guard.AgainstNull(services);

        var azureStorageQueuesBuilder = new AzureStorageQueueBuilder(services);

        builder?.Invoke(azureStorageQueuesBuilder);

        services.AddSingleton<IValidateOptions<AzureStorageQueueOptions>, AzureStorageQueueOptionsValidator>();

        foreach (var pair in azureStorageQueuesBuilder.AzureStorageQueueOptions)
        {
            services.AddOptions<AzureStorageQueueOptions>(pair.Key).Configure(options =>
            {
                options.StorageAccount = pair.Value.StorageAccount;
                options.ConnectionString = pair.Value.ConnectionString;
                options.VisibilityTimeout = pair.Value.VisibilityTimeout;
                options.MaxMessages = pair.Value.MaxMessages;

                if (options.MaxMessages < 1)
                {
                    options.MaxMessages = 1;
                }

                if (options.MaxMessages > 32)
                {
                    options.MaxMessages = 32;
                }

                options.Configure += (sender, args) =>
                {
                    pair.Value.OnConfigureConsumer(sender, args);
                };
            });
        }

        services.TryAddSingleton<IQueueFactory, AzureStorageQueueFactory>();

        return services;
    }
}