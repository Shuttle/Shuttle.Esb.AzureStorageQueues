using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Shuttle.Core.Contract;
using Shuttle.Core.Streams;

namespace Shuttle.Esb.AzureStorageQueues
{
    public class AzureStorageQueue : IQueue, ICreateQueue, IDropQueue, IDisposable, IPurgeQueue
    {
        internal class AcknowledgementToken
        {
            public string MessageId { get; }
            public string MessageText { get; }
            public string PopReceipt { get; }

            public AcknowledgementToken(string messageId, string messageText, string popReceipt)
            {
                MessageId = messageId;
                MessageText = messageText;
                PopReceipt = popReceipt;
            }
        }

        private readonly AzureStorageQueueOptions _azureStorageQueueOptions;
        private readonly CancellationToken _cancellationToken;
        private readonly TimeSpan _infiniteTimeToLive = new TimeSpan(0, 0, -1);

        private readonly Dictionary<string, AcknowledgementToken> _acknowledgementTokens = new Dictionary<string, AcknowledgementToken>();
        private readonly Queue<ReceivedMessage> _receivedMessages = new Queue<ReceivedMessage>();
        private readonly object _lock = new object();

        private readonly QueueClient _queueClient;

        public AzureStorageQueue(QueueUri uri, AzureStorageQueueOptions azureStorageQueueOptions, CancellationToken cancellationToken)
        {
            Guard.AgainstNull(uri, nameof(uri));
            Guard.AgainstNull(azureStorageQueueOptions, nameof(azureStorageQueueOptions));

            _cancellationToken = cancellationToken;

            Uri = uri;

            _azureStorageQueueOptions = azureStorageQueueOptions;

            var queueClientOptions = new QueueClientOptions();

            _azureStorageQueueOptions.OnConfigureConsumer(this, new ConfigureEventArgs(queueClientOptions));

            _queueClient = new QueueClient(_azureStorageQueueOptions.ConnectionString, Uri.QueueName, queueClientOptions);
        }

        public bool IsEmpty()
        {
            lock (_lock)
            {
                return ((QueueProperties) _queueClient.GetProperties()).ApproximateMessagesCount == 0;
            }
        }

        public void Enqueue(TransportMessage message, Stream stream)
        {
            Guard.AgainstNull(message, nameof(message));
            Guard.AgainstNull(stream, nameof(stream));

            lock (_lock)
            {
                try
                {
                    _queueClient.SendMessage(Convert.ToBase64String(stream.ToBytes()), null, _infiniteTimeToLive, _cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        public ReceivedMessage GetMessage()
        {
            lock (_lock)
            {
                if (_receivedMessages.Count == 0)
                {
                    Response<QueueMessage[]> messages = null;

                    try
                    {
                        messages = _queueClient.ReceiveMessages(_azureStorageQueueOptions.MaxMessages, _azureStorageQueueOptions.VisibilityTimeout, _cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    if (messages == null)
                    {
                        return null;
                    }

                    foreach (var message in messages.Value)
                    {
                        var acknowledgementToken = new AcknowledgementToken(message.MessageId, message.MessageText,  message.PopReceipt);

                        _acknowledgementTokens.Add(acknowledgementToken.MessageId, acknowledgementToken);

                        _receivedMessages.Enqueue(new ReceivedMessage(
                            new MemoryStream(Convert.FromBase64String(message.MessageText)),
                            acknowledgementToken));
                    }
                }

                return _receivedMessages.Count > 0 ? _receivedMessages.Dequeue() : null;
            }
        }

        public void Acknowledge(object acknowledgementToken)
        {
            Guard.AgainstNull(acknowledgementToken, nameof(acknowledgementToken));

            lock (_lock)
            {
                if (!(acknowledgementToken is AcknowledgementToken data))
                {
                    return;
                }

                if (_acknowledgementTokens.ContainsKey(data.MessageId))
                {
                    _acknowledgementTokens.Remove(data.MessageId);
                }

                try
                {
                    _queueClient.DeleteMessage(data.MessageId, data.PopReceipt, _cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        public void Release(object acknowledgementToken)
        {
            Guard.AgainstNull(acknowledgementToken, nameof(acknowledgementToken));

            if (!(acknowledgementToken is AcknowledgementToken data))
            {
                return;
            }

            lock (_lock)
            {
                try
                {
                    _queueClient.SendMessage(data.MessageText, _cancellationToken);
                    _queueClient.DeleteMessage(data.MessageId, data.PopReceipt, _cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }

                if (_acknowledgementTokens.ContainsKey(data.MessageId))
                {
                    _acknowledgementTokens.Remove(data.MessageId);
                }
            }
        }

        public QueueUri Uri { get; }
        public bool IsStream => false;

        public void Create()
        {
            lock (_lock)
            {
                try
                {
                    _queueClient.CreateIfNotExists(null, _cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        public void Drop()
        {
            lock (_lock)
            {
                try
                {
                    _queueClient.DeleteIfExists(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var acknowledgementToken in _acknowledgementTokens.Values)
                {
                    _queueClient.SendMessage(acknowledgementToken.MessageText);
                    _queueClient.DeleteMessage(acknowledgementToken.MessageId, acknowledgementToken.PopReceipt);
                }

                _acknowledgementTokens.Clear();
            }
        }

        public void Purge()
        {
            lock (_lock)
            {
                try
                {
                    _queueClient.ClearMessages(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }
}
