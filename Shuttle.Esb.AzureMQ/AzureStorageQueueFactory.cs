using System;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureMQ
{
    public class AzureStorageQueueFactory : IQueueFactory
    {
        private readonly IOptionsMonitor<ConnectionStringSettings> _connectionStringOptions;
        public string Scheme => AzureStorageQueueUriParser.Scheme;

        public AzureStorageQueueFactory(IOptionsMonitor<ConnectionStringSettings> connectionStringOptions)
        {
            Guard.AgainstNull(connectionStringOptions, nameof(connectionStringOptions));

            _connectionStringOptions = connectionStringOptions;
        }

        public IQueue Create(Uri uri)
        {
            Guard.AgainstNull(uri, "uri");

            return new AzureStorageQueue(uri, _connectionStringOptions);
        }
    }
}