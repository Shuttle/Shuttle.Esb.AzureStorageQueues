using System;
using System.Collections.Generic;
using Shuttle.Core.Configuration;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureMQ
{
    public class AzureMQConfiguration : IAzureMQConfiguration
    {
        private readonly Dictionary<string, string> _connectionStrings = new Dictionary<string, string>();

        public string GetConnectionString(string name)
        {
            Guard.AgainstNullOrEmptyString(name, nameof(name));

            if (!_connectionStrings.ContainsKey(name))
            {
                throw new InvalidOperationException(string.Format(Resources.UnknownConnectionStringException, name));
            }

            return _connectionStrings[name];
        }

        public void AddConnectionString(string name, string connectionString)
        {
            Guard.AgainstNullOrEmptyString(name, nameof(name));
            Guard.AgainstNullOrEmptyString(connectionString, nameof(connectionString));

            _connectionStrings[name] = connectionString;
        }
    }
}