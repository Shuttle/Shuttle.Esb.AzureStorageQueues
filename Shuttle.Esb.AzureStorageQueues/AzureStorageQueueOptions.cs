﻿using System;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureStorageQueues
{
    public class AzureStorageQueueOptions
    {
        public const string SectionName = "Shuttle:ServiceBus:AzureStorageQueues";

        public string ConnectionString { get; set; }
        public int MaxMessages { get; set; }

        public event EventHandler<ConfigureEventArgs> Configure = delegate
        {
        };

        public void OnConfigureConsumer(object sender, ConfigureEventArgs args)
        {
            Guard.AgainstNull(sender, nameof(sender));
            Guard.AgainstNull(args, nameof(args));

            Configure.Invoke(sender, args);
        }
    }
}