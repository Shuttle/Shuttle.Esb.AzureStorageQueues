﻿using System;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Threading;

namespace Shuttle.Esb.AzureStorageQueues;

public class AzureStorageQueueFactory : IQueueFactory
{
    private readonly IOptionsMonitor<AzureStorageQueueOptions> _azureStorageQueueOptions;
    private readonly ICancellationTokenSource _cancellationTokenSource;

    public AzureStorageQueueFactory(IOptionsMonitor<AzureStorageQueueOptions> azureStorageQueueOptions, ICancellationTokenSource cancellationTokenSource)
    {
        _azureStorageQueueOptions = Guard.AgainstNull(azureStorageQueueOptions);
        _cancellationTokenSource = Guard.AgainstNull(cancellationTokenSource);
    }

    public string Scheme => "azuresq";

    public IQueue Create(Uri uri)
    {
        var queueUri = new QueueUri(Guard.AgainstNull(uri)).SchemeInvariant(Scheme);
        var azureStorageQueueOptions = _azureStorageQueueOptions.Get(queueUri.ConfigurationName);

        if (azureStorageQueueOptions == null)
        {
            throw new InvalidOperationException(string.Format(Esb.Resources.QueueConfigurationNameException, queueUri.ConfigurationName));
        }

        return new AzureStorageQueue(queueUri, azureStorageQueueOptions, _cancellationTokenSource.Get().Token);
    }
}