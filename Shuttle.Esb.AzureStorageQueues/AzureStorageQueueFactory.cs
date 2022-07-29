using System;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Threading;

namespace Shuttle.Esb.AzureStorageQueues
{
    public class AzureStorageQueueFactory : IQueueFactory
    {
        private readonly IOptionsMonitor<AzureStorageQueueOptions> _azureStorageQueueOptions;
        private readonly ICancellationTokenSource _cancellationTokenSource;
        public string Scheme => "azuresq";

        public AzureStorageQueueFactory(IOptionsMonitor<AzureStorageQueueOptions> azureStorageQueueOptions, ICancellationTokenSource cancellationTokenSource)
        {
            Guard.AgainstNull(azureStorageQueueOptions, nameof(azureStorageQueueOptions));
            Guard.AgainstNull(cancellationTokenSource, nameof(cancellationTokenSource));

            _azureStorageQueueOptions = azureStorageQueueOptions;
            _cancellationTokenSource = cancellationTokenSource;
        }

        public IQueue Create(Uri uri)
        {
            Guard.AgainstNull(uri, "uri");

            var queueUri = new QueueUri(uri).SchemeInvariant(Scheme);
            var azureStorageQueueOptions = _azureStorageQueueOptions.Get(queueUri.ConfigurationName);

            if (azureStorageQueueOptions == null)
            {
                throw new InvalidOperationException(string.Format(Esb.Resources.QueueConfigurationNameException, queueUri.ConfigurationName));
            }

            return new AzureStorageQueue(queueUri, azureStorageQueueOptions, _cancellationTokenSource.Get().Token);
        }
    }
}