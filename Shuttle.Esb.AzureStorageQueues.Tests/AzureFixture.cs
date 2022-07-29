using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                builder.AddOptions("azure", new AzureStorageQueueOptions
                {
                    ConnectionString = "UseDevelopmentStorage=true"
                });
            });

            return services;
        }
    }
}           
