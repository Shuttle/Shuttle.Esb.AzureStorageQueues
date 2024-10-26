using System;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureStorageQueues;

public class AzureStorageQueueOptions
{
    public const string SectionName = "Shuttle:AzureStorageQueues";
    public string ConnectionString { get; set; } = string.Empty;
    public int MaxMessages { get; set; } = 32;

    public string StorageAccount { get; set; } = string.Empty;
    public TimeSpan? VisibilityTimeout { get; set; }

    public event EventHandler<ConfigureEventArgs>? Configure;

    public void OnConfigureConsumer(object? sender, ConfigureEventArgs args)
    {
        Configure?.Invoke(Guard.AgainstNull(sender), Guard.AgainstNull(args));
    }
}