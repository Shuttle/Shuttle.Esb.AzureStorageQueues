using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shuttle.Core.Pipelines;
using Shuttle.Esb.Logging;

namespace Shuttle.Esb.AzureStorageQueues.Tests
{
    public static class AzureFixture
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

                azureStorageQueueOptions.Configure += (sender, args) =>
                {
                    Console.WriteLine($"[event] : Configure / Uri = '{((IQueue)sender).Uri}'");
                };

                builder.AddOptions("azure", azureStorageQueueOptions);
            });

            return services;
        }
    }
}           
