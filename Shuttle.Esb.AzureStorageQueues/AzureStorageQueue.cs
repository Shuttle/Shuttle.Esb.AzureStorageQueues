using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1,1);

        private readonly QueueClient _queueClient;

        public AzureStorageQueue(QueueUri uri, AzureStorageQueueOptions azureStorageQueueOptions, CancellationToken cancellationToken)
        {
            Uri = Guard.AgainstNull(uri, nameof(uri));
            _azureStorageQueueOptions = Guard.AgainstNull(azureStorageQueueOptions, nameof(azureStorageQueueOptions));

            _cancellationToken = cancellationToken;

            var queueClientOptions = new QueueClientOptions();

            _azureStorageQueueOptions.OnConfigureConsumer(this, new ConfigureEventArgs(queueClientOptions));

            _queueClient = new QueueClient(_azureStorageQueueOptions.ConnectionString, Uri.QueueName, queueClientOptions);
        }

        public async ValueTask<bool> IsEmpty()
        {
            await _lock.WaitAsync(CancellationToken.None);

            try
            {
                return ((QueueProperties)await _queueClient.GetPropertiesAsync(_cancellationToken).ConfigureAwait(false)).ApproximateMessagesCount == 0;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Enqueue(TransportMessage message, Stream stream)
        {
            Guard.AgainstNull(message, nameof(message));
            Guard.AgainstNull(stream, nameof(stream));

            await _lock.WaitAsync(CancellationToken.None);

            try
            {
                await _queueClient.SendMessageAsync(Convert.ToBase64String(await stream.ToBytesAsync().ConfigureAwait(false)), null, _infiniteTimeToLive, _cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<ReceivedMessage> GetMessage()
        {
            await _lock.WaitAsync(CancellationToken.None);

            try
            {
                if (_receivedMessages.Count == 0)
                {
                    Response<QueueMessage[]> messages = null;

                    try
                    {
                        messages = await _queueClient.ReceiveMessagesAsync(_azureStorageQueueOptions.MaxMessages, _azureStorageQueueOptions.VisibilityTimeout, _cancellationToken).ConfigureAwait(false);
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
                        var acknowledgementToken = new AcknowledgementToken(message.MessageId, message.MessageText, message.PopReceipt);

                        _acknowledgementTokens.Add(acknowledgementToken.MessageId, acknowledgementToken);

                        _receivedMessages.Enqueue(new ReceivedMessage(
                            new MemoryStream(Convert.FromBase64String(message.MessageText)),
                            acknowledgementToken));
                    }
                }

                return _receivedMessages.Count > 0 ? _receivedMessages.Dequeue() : null;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Acknowledge(object acknowledgementToken)
        {
            Guard.AgainstNull(acknowledgementToken, nameof(acknowledgementToken));

            await _lock.WaitAsync(CancellationToken.None);

            try
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
                    await _queueClient.DeleteMessageAsync(data.MessageId, data.PopReceipt, _cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Release(object acknowledgementToken)
        {
            Guard.AgainstNull(acknowledgementToken, nameof(acknowledgementToken));

            if (!(acknowledgementToken is AcknowledgementToken data))
            {
                return;
            }

            await _lock.WaitAsync(CancellationToken.None);

            try
            {
                try
                {
                    await _queueClient.SendMessageAsync(data.MessageText, _cancellationToken).ConfigureAwait(false);
                    await _queueClient.DeleteMessageAsync(data.MessageId, data.PopReceipt, _cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }

                if (_acknowledgementTokens.ContainsKey(data.MessageId))
                {
                    _acknowledgementTokens.Remove(data.MessageId);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public QueueUri Uri { get; }
        public bool IsStream => false;

        public async Task Create()
        {
            await _lock.WaitAsync(CancellationToken.None);

            try
            {
                try
                {
                    await _queueClient.CreateIfNotExistsAsync(null, _cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Drop()
        {
            await _lock.WaitAsync(CancellationToken.None);

            try
            {
                try
                {
                    await _queueClient.DeleteIfExistsAsync(_cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            _lock.Wait(CancellationToken.None);

            try
            {
                foreach (var acknowledgementToken in _acknowledgementTokens.Values)
                {
                    _queueClient.SendMessage(acknowledgementToken.MessageText);
                    _queueClient.DeleteMessage(acknowledgementToken.MessageId, acknowledgementToken.PopReceipt);
                }

                _acknowledgementTokens.Clear();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Purge()
        {
            await _lock.WaitAsync(CancellationToken.None);

            try
            {
                try
                {
                    await _queueClient.ClearMessagesAsync(_cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
