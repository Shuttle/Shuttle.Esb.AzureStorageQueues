using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureMQ
{
    public class AzureMQConfigurationBuilder
    {
        private readonly IServiceCollection _services;
        private readonly AzureMQConfiguration _configuration = new AzureMQConfiguration();

        public AzureMQConfigurationBuilder(IServiceCollection services)
        {
            Guard.AgainstNull(services, nameof(services));
            
            _services = services;
        }

        public IAzureMQConfiguration GetConfiguration()
        {
            return _configuration;
        }

        public AzureMQConfigurationBuilder AddConnectionString(string name)
        {
            _services.AddOptions<ConnectionStringSettings>(name).Configure<IConfiguration>((option, configuration) =>
            {
                var connectionString = configuration.GetConnectionString(name);

                Guard.AgainstNullOrEmptyString(connectionString, nameof(connectionString));

                option.ConnectionString = connectionString;
                option.Name = name;

                _configuration.AddConnectionString(name, option.ConnectionString);
            });

            return this;
        }

        public AzureMQConfigurationBuilder AddConnectionString(string name, string connectionString)
        {
            _configuration.AddConnectionString(name, connectionString);

            return this;
        }
    }
}