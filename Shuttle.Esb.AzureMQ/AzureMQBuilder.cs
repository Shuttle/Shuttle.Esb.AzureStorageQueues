using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureMQ
{
    public class AzureMQBuilder
    {
        public IServiceCollection Services { get; }

        public AzureMQBuilder(IServiceCollection services)
        {
            Guard.AgainstNull(services, nameof(services));
            
            Services = services;
        }

        public AzureMQBuilder AddConnectionString(string name)
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

        public AzureMQBuilder AddConnectionString(string name, string connectionString)
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