using System;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureMQ
{
    public class AzureMQConfiguration : IAzureMQConfiguration
    {
        private readonly IOptionsMonitor<ConnectionStringSettings> _connectionStringOptions;

        public AzureMQConfiguration(IOptionsMonitor<ConnectionStringSettings> connectionStringOptions)
        {
            Guard.AgainstNull(connectionStringOptions, nameof(connectionStringOptions));

            _connectionStringOptions = connectionStringOptions;
        }

        public string GetConnectionString(string name)
        {
            Guard.AgainstNullOrEmptyString(name, nameof(name));

            var connectionStringSettings = _connectionStringOptions.Get(name);

            if (connectionStringSettings == null)
            {
                throw new InvalidOperationException(string.Format(Resources.UnknownConnectionStringException, name));
            }

            return connectionStringSettings.ConnectionString;
        }
    }
}