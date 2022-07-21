using System;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Threading;

namespace Shuttle.Esb.AzureStorageQueues
{
    public class AzureStorageQueueFactory : IQueueFactory
    {
        private readonly IOptionsMonitor<ConnectionStringOptions> _connectionStringOptions;
        private readonly ICancellationTokenSource _cancellationTokenSource;
        public string Scheme => AzureStorageQueueUriParser.Scheme;

        public AzureStorageQueueFactory(IOptionsMonitor<ConnectionStringOptions> connectionStringOptions, ICancellationTokenSource cancellationTokenSource)
        {
            Guard.AgainstNull(connectionStringOptions, nameof(connectionStringOptions));
            Guard.AgainstNull(cancellationTokenSource, nameof(cancellationTokenSource));

            _connectionStringOptions = connectionStringOptions;
            _cancellationTokenSource = cancellationTokenSource;
        }

        public IQueue Create(Uri uri)
        {
            Guard.AgainstNull(uri, "uri");

            return new AzureStorageQueue(uri, _connectionStringOptions, _cancellationTokenSource.Get().Token);
        }
    }
}