using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureMQ
{
    public class AzureMQBuilder
    {
        private readonly IServiceCollection _services;

        public AzureMQBuilder(IServiceCollection services)
        {
            Guard.AgainstNull(services, nameof(services));
            
            _services = services;
        }

        public AzureMQBuilder AddConnectionString(string name)
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

        public AzureMQBuilder AddConnectionString(string name, string connectionString)
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