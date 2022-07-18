using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureStorageQueues
{
    public class AzureStorageQueuesBuilder
    {
        public IServiceCollection Services { get; }

        public AzureStorageQueuesBuilder(IServiceCollection services)
        {
            Guard.AgainstNull(services, nameof(services));
            
            Services = services;
        }

        public AzureStorageQueuesBuilder AddConnectionString(string name)
        {
            Services.AddOptions<ConnectionStringOptions>(name).Configure<IConfiguration>((option, configuration) =>
            {
                var connectionString = configuration.GetConnectionString(name);

                Guard.AgainstNullOrEmptyString(connectionString, nameof(connectionString));

                option.ConnectionString = connectionString;
                option.Name = name;
            });

            return this;
        }

        public AzureStorageQueuesBuilder AddConnectionString(string name, string connectionString)
        {
            Services.AddOptions<ConnectionStringOptions>(name).Configure(option =>
            {
                Guard.AgainstNullOrEmptyString(connectionString, nameof(connectionString));

                option.ConnectionString = connectionString;
                option.Name = name;
            });

            return this;
        }
    }
}