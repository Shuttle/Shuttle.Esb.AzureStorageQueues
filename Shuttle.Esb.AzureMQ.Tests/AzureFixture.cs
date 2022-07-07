using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shuttle.Esb.AzureMQ.Tests
{
    public static class AzureFixture
    {
        public static IServiceCollection GetServiceCollection()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddAzureStorageQueues(builder =>
            {
                builder.AddConnectionString("azure", "UseDevelopmentStorage=true");
            });

            return services;
        }
    }
}           
