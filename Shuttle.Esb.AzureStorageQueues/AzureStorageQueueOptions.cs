using System;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureStorageQueues
{
    public class AzureStorageQueueOptions
    {
        public const string SectionName = "Shuttle:AzureStorageQueues";

        public string ConnectionString { get; set; }
        public int MaxMessages { get; set; } = 32;
        public TimeSpan? VisibilityTimeout { get; set; }

        public event EventHandler<ConfigureEventArgs> Configure;

        public void OnConfigureConsumer(object sender, ConfigureEventArgs args)
        {
            Guard.AgainstNull(sender, nameof(sender));
            Guard.AgainstNull(args, nameof(args));

            Configure?.Invoke(sender, args);
        }
    }
}