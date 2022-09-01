using Azure.Storage.Queues;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureStorageQueues
{
    public class ConfigureEventArgs
    {
        private QueueClientOptions _queueClientOptions;

        public QueueClientOptions QueueClientOptions
        {
            get => _queueClientOptions;
            set => _queueClientOptions = value ?? throw new System.ArgumentNullException();
        }

        public ConfigureEventArgs(QueueClientOptions queueClientOptions)
        {
            Guard.AgainstNull(queueClientOptions, nameof(queueClientOptions));

            _queueClientOptions = queueClientOptions;
        }
    }
}