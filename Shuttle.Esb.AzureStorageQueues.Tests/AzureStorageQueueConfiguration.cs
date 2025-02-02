using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shuttle.Esb.AzureStorageQueues.Tests;

public static class AzureStorageQueueConfiguration
{
    public static IServiceCollection GetServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddAzureStorageQueues(builder =>
        {
            var azureStorageQueueOptions = new AzureStorageQueueOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                MaxMessages = 20,
                VisibilityTimeout = null
            };

            azureStorageQueueOptions.Configure += (sender, _) =>
            {
                Console.WriteLine($@"[event] : Configure / Uri = '{(sender as IQueue)?.Uri}'");
            };

            builder.AddOptions("azure", azureStorageQueueOptions);
        });

        return services;
    }
}