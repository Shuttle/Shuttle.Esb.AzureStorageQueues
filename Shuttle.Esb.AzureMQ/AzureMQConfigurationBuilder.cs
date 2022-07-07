using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureMQ
{
    public class AzureMQConfigurationBuilder
    {
        private readonly IServiceCollection _services;

        public AzureMQConfigurationBuilder(IServiceCollection services)
        {
            Guard.AgainstNull(services, nameof(services));
            
            _services = services;
        }

        public AzureMQConfigurationBuilder AddConnectionString(string name)
        {
            _services.AddOptions<ConnectionStringSettings>(name).Configure<IConfiguration>((option, configuration) =>
            {
                var connectionString = configuration.GetConnectionString(name);

                Guard.AgainstNullOrEmptyString(connectionString, nameof(connectionString));

                option.ConnectionString = connectionString;
                option.Name = name;
            });

            return this;
        }

        public AzureMQConfigurationBuilder AddConnectionString(string name, string connectionString)
        {
            _services.AddOptions<ConnectionStringSettings>(name).Configure(option =>
            {
                Guard.AgainstNullOrEmptyString(connectionString, nameof(connectionString));

                option.ConnectionString = connectionString;
                option.Name = name;
            });

            return this;
        }
    }
}