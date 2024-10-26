using System;
using Azure.Storage.Queues;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureStorageQueues;

public class ConfigureEventArgs
{
    private QueueClientOptions _queueClientOptions;

    public ConfigureEventArgs(QueueClientOptions queueClientOptions)
    {
        _queueClientOptions = Guard.AgainstNull(queueClientOptions);
    }

    public QueueClientOptions QueueClientOptions
    {
        get => _queueClientOptions;
        set => _queueClientOptions = value ?? throw new ArgumentNullException();
    }
}