using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Shuttle.Core.Contract;
using Shuttle.Core.Streams;

namespace Shuttle.Esb.AzureStorageQueues
{
    public class AzureStorageQueue : IQueue, ICreateQueue, IDropQueue, IDisposable, IPurgeQueue
    {
        private readonly Dictionary<string, AcknowledgementToken> _acknowledgementTokens = new Dictionary<string, AcknowledgementToken>();

        private readonly AzureStorageQueueOptions _azureStorageQueueOptions;
        private readonly CancellationToken _cancellationToken;
        private readonly TimeSpan _infiniteTimeToLive = new TimeSpan(0, 0, -1);
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        private readonly QueueClient _queueClient;
        private readonly Queue<ReceivedMessage> _receivedMessages = new Queue<ReceivedMessage>();

        public AzureStorageQueue(QueueUri uri, AzureStorageQueueOptions azureStorageQueueOptions, CancellationToken cancellationToken)
        {
            Uri = Guard.AgainstNull(uri, nameof(uri));
            _azureStorageQueueOptions = Guard.AgainstNull(azureStorageQueueOptions, nameof(azureStorageQueueOptions));

            _cancellationToken = cancellationToken;

            var queueClientOptions = new QueueClientOptions();

            _azureStorageQueueOptions.OnConfigureConsumer(this, new ConfigureEventArgs(queueClientOptions));

            if (!string.IsNullOrWhiteSpace(_azureStorageQueueOptions.ConnectionString))
            {
                _queueClient = new QueueClient(_azureStorageQueueOptions.ConnectionString, Uri.QueueName, queueClientOptions);
            }

            if (!string.IsNullOrWhiteSpace(_azureStorageQueueOptions.StorageAccount))
            {
                _queueClient = new QueueClient(new Uri($"https://{_azureStorageQueueOptions.StorageAccount}.queue.core.windows.net/{Uri.QueueName}"), new DefaultAzureCredential());
            }

            if (_queueClient == null)
            {
                throw new InvalidOperationException(string.Format(Resources.QueueUriException, uri.ConfigurationName));
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

        public event EventHandler<MessageEnqueuedEventArgs> MessageEnqueued;
        public event EventHandler<MessageAcknowledgedEventArgs> MessageAcknowledged;
        public event EventHandler<MessageReleasedEventArgs> MessageReleased;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<OperationEventArgs> Operation;

        public bool IsEmpty()
        {
            return IsEmptyAsync(true).GetAwaiter().GetResult();
        }

        public async ValueTask<bool> IsEmptyAsync()
        {
            return await IsEmptyAsync(false).ConfigureAwait(false);
        }

        public void Enqueue(TransportMessage transportMessage, Stream stream)
        {
            EnqueueAsync(transportMessage, stream, true).GetAwaiter().GetResult();
        }

        public async Task EnqueueAsync(TransportMessage transportMessage, Stream stream)
        {
            await EnqueueAsync(transportMessage, stream, false).ConfigureAwait(false);
        }

        public ReceivedMessage GetMessage()
        {
            return GetMessageAsync(true).GetAwaiter().GetResult();
        }

        public async Task<ReceivedMessage> GetMessageAsync()
        {
            return await GetMessageAsync(false).ConfigureAwait(false);
        }

        public void Acknowledge(object acknowledgementToken)
        {
            AcknowledgeAsync(acknowledgementToken, true).GetAwaiter().GetResult();
        }

        public async Task AcknowledgeAsync(object acknowledgementToken)
        {
            await AcknowledgeAsync(acknowledgementToken, false).ConfigureAwait(false);
        }

        public void Release(object acknowledgementToken)
        {
            ReleaseAsync(acknowledgementToken, true).GetAwaiter().GetResult();
        }

        public async Task ReleaseAsync(object acknowledgementToken)
        {
            await ReleaseAsync(acknowledgementToken, false).ConfigureAwait(false);
        }

        public QueueUri Uri { get; }
        public bool IsStream => false;

        private async Task AcknowledgeAsync(object acknowledgementToken, bool sync)
        {
            Guard.AgainstNull(acknowledgementToken, nameof(acknowledgementToken));

            if (_cancellationToken.IsCancellationRequested)
            {
                Operation?.Invoke(this, new OperationEventArgs("[acknowledge/cancelled]"));
                return;
            }

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            if (!(acknowledgementToken is AcknowledgementToken data))
            {
                return;
            }

            try
            {
                if (sync)
                {
                    _queueClient.DeleteMessage(data.MessageId, data.PopReceipt, _cancellationToken);
                }
                else
                {
                    await _queueClient.DeleteMessageAsync(data.MessageId, data.PopReceipt, _cancellationToken).ConfigureAwait(false);
                }

                MessageAcknowledged?.Invoke(this, new MessageAcknowledgedEventArgs(acknowledgementToken));
            }
            catch (OperationCanceledException)
            {
                Operation?.Invoke(this, new OperationEventArgs("[acknowledge/cancelled]"));
            }
            finally
            {
                _lock.Release();
            }

            if (_acknowledgementTokens.ContainsKey(data.MessageId))
            {
                _acknowledgementTokens.Remove(data.MessageId);
            }
        }

        private async Task CreateAsync(bool sync)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                Operation?.Invoke(this, new OperationEventArgs("[create/cancelled]"));
                return;
            }

            Operation?.Invoke(this, new OperationEventArgs("[create/starting]"));

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                if (sync)
                {
                    _queueClient.CreateIfNotExists(null, _cancellationToken);
                }
                else
                {
                    await _queueClient.CreateIfNotExistsAsync(null, _cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Operation?.Invoke(this, new OperationEventArgs("[create/cancelled]"));
            }
            finally
            {
                _lock.Release();
            }

            Operation?.Invoke(this, new OperationEventArgs("[create/completed]"));
        }

        private async Task DropAsync(bool sync)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                Operation?.Invoke(this, new OperationEventArgs("[drop/cancelled]"));
                return;
            }

            Operation?.Invoke(this, new OperationEventArgs("[drop/starting]"));

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                if (sync)
                {
                    _queueClient.DeleteIfExists(_cancellationToken);
                }
                else
                {
                    await _queueClient.DeleteIfExistsAsync(_cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Operation?.Invoke(this, new OperationEventArgs("[drop/cancelled]"));
            }
            finally
            {
                _lock.Release();
            }

            Operation?.Invoke(this, new OperationEventArgs("[drop/completed]"));
        }

        private async Task EnqueueAsync(TransportMessage message, Stream stream, bool sync)
        {
            Guard.AgainstNull(message, nameof(message));
            Guard.AgainstNull(stream, nameof(stream));

            if (_cancellationToken.IsCancellationRequested)
            {
                Operation?.Invoke(this, new OperationEventArgs("[enqueue/cancelled]"));
                return;
            }

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                if (sync)
                {
                    _queueClient.SendMessage(Convert.ToBase64String(stream.ToBytes()), null, _infiniteTimeToLive, _cancellationToken);
                }
                else
                {
                    await _queueClient.SendMessageAsync(Convert.ToBase64String(await stream.ToBytesAsync().ConfigureAwait(false)), null, _infiniteTimeToLive, _cancellationToken).ConfigureAwait(false);
                }

                MessageEnqueued?.Invoke(this, new MessageEnqueuedEventArgs(message, stream));
            }
            catch (OperationCanceledException)
            {
                Operation?.Invoke(this, new OperationEventArgs("[enqueue/cancelled]"));
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<ReceivedMessage> GetMessageAsync(bool sync)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                Operation?.Invoke(this, new OperationEventArgs("[get-message/cancelled]"));
                return null;
            }

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                if (_receivedMessages.Count == 0)
                {
                    Response<QueueMessage[]> messages = null;

                    try
                    {
                        messages = sync
                            ? _queueClient.ReceiveMessages(_azureStorageQueueOptions.MaxMessages, _azureStorageQueueOptions.VisibilityTimeout, _cancellationToken)
                            : await _queueClient.ReceiveMessagesAsync(_azureStorageQueueOptions.MaxMessages, _azureStorageQueueOptions.VisibilityTimeout, _cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        Operation?.Invoke(this, new OperationEventArgs("[get-message/cancelled]"));
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

                var receivedMessage = _receivedMessages.Count > 0 ? _receivedMessages.Dequeue() : null;

                if (receivedMessage != null)
                {
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(receivedMessage));
                }

                return receivedMessage;
            }
            catch (OperationCanceledException)
            {
                Operation?.Invoke(this, new OperationEventArgs("[get-message/cancelled]"));
            }
            finally
            {
                _lock.Release();
            }

            return null;
        }

        private async ValueTask<bool> IsEmptyAsync(bool sync)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                Operation?.Invoke(this, new OperationEventArgs("[is-empty/cancelled]", true));
                return true;
            }

            Operation?.Invoke(this, new OperationEventArgs("[is-empty/starting]"));

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                var result = sync
                    ? ((QueueProperties)_queueClient.GetProperties(_cancellationToken)).ApproximateMessagesCount == 0
                    : ((QueueProperties)await _queueClient.GetPropertiesAsync(_cancellationToken).ConfigureAwait(false)).ApproximateMessagesCount == 0;

                Operation?.Invoke(this, new OperationEventArgs("[is-empty]", result));

                return result;
            }
            catch (OperationCanceledException)
            {
                Operation?.Invoke(this, new OperationEventArgs("[is-empty/cancelled]", true));
            }
            finally
            {
                _lock.Release();
            }

            return true;
        }

        private async Task PurgeAsync(bool sync)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                Operation?.Invoke(this, new OperationEventArgs("[purge/cancelled]"));
                return;
            }

            Operation?.Invoke(this, new OperationEventArgs("[purge/starting]"));

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                if (sync)
                {
                    _queueClient.ClearMessages(_cancellationToken);
                }
                else
                {
                    await _queueClient.ClearMessagesAsync(_cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Operation?.Invoke(this, new OperationEventArgs("[purge/cancelled]"));
            }
            finally
            {
                _lock.Release();
            }

            Operation?.Invoke(this, new OperationEventArgs("[purge/completed]"));
        }

        private async Task ReleaseAsync(object acknowledgementToken, bool sync)
        {
            Guard.AgainstNull(acknowledgementToken, nameof(acknowledgementToken));

            if (!(acknowledgementToken is AcknowledgementToken data))
            {
                return;
            }

            if (_cancellationToken.IsCancellationRequested)
            {
                Operation?.Invoke(this, new OperationEventArgs("[release/cancelled]"));
                return;
            }

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                if (sync)
                {
                    _queueClient.SendMessage(data.MessageText, _cancellationToken);
                    _queueClient.DeleteMessage(data.MessageId, data.PopReceipt, _cancellationToken);
                }
                else
                {
                    await _queueClient.SendMessageAsync(data.MessageText, _cancellationToken).ConfigureAwait(false);
                    await _queueClient.DeleteMessageAsync(data.MessageId, data.PopReceipt, _cancellationToken).ConfigureAwait(false);
                }

                MessageReleased?.Invoke(this, new MessageReleasedEventArgs(acknowledgementToken));
            }
            catch (OperationCanceledException)
            {
                Operation?.Invoke(this, new OperationEventArgs("[release/cancelled]"));
            }
            finally
            {
                _lock.Release();
            }

            if (_acknowledgementTokens.ContainsKey(data.MessageId))
            {
                _acknowledgementTokens.Remove(data.MessageId);
            }
        }

        internal class AcknowledgementToken
        {
            public AcknowledgementToken(string messageId, string messageText, string popReceipt)
            {
                MessageId = messageId;
                MessageText = messageText;
                PopReceipt = popReceipt;
            }

            public string MessageId { get; }
            public string MessageText { get; }
            public string PopReceipt { get; }
        }

        public void Create()
        {
            CreateAsync(true).GetAwaiter().GetResult();
        }

        public async Task CreateAsync()
        {
            await CreateAsync(false).ConfigureAwait(false);
        }

        public void Drop()
        {
            DropAsync(true).GetAwaiter().GetResult();
        }

        public async Task DropAsync()
        {
            await DropAsync(false).ConfigureAwait(false);
        }

        public void Purge()
        {
            PurgeAsync(true).GetAwaiter().GetResult();
        }

        public async Task PurgeAsync()
        {
            await PurgeAsync(false).ConfigureAwait(false);
        }
    }
}