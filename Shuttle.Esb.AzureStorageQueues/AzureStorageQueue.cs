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

namespace Shuttle.Esb.AzureStorageQueues;

public class AzureStorageQueue : IQueue, ICreateQueue, IDropQueue, IDisposable, IPurgeQueue
{
    private readonly Dictionary<string, AcknowledgementToken> _acknowledgementTokens = new();

    private readonly AzureStorageQueueOptions _azureStorageQueueOptions;
    private readonly CancellationToken _cancellationToken;
    private readonly TimeSpan _infiniteTimeToLive = new(0, 0, -1);
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly QueueClient _queueClient;
    private readonly Queue<ReceivedMessage> _receivedMessages = new();

    public AzureStorageQueue(QueueUri uri, AzureStorageQueueOptions azureStorageQueueOptions, CancellationToken cancellationToken)
    {
        Uri = Guard.AgainstNull(uri);
        _azureStorageQueueOptions = Guard.AgainstNull(azureStorageQueueOptions);

        _cancellationToken = cancellationToken;

        var queueClientOptions = new QueueClientOptions();

        _azureStorageQueueOptions.OnConfigureConsumer(this, new(queueClientOptions));

        if (!string.IsNullOrWhiteSpace(_azureStorageQueueOptions.ConnectionString))
        {
            _queueClient = new(_azureStorageQueueOptions.ConnectionString, Uri.QueueName, queueClientOptions);
        }

        if (!string.IsNullOrWhiteSpace(_azureStorageQueueOptions.StorageAccount))
        {
            _queueClient = new(new($"https://{_azureStorageQueueOptions.StorageAccount}.queue.core.windows.net/{Uri.QueueName}"), new DefaultAzureCredential());
        }

        if (_queueClient == null)
        {
            throw new InvalidOperationException(string.Format(Resources.QueueUriException, uri.ConfigurationName));
        }
    }

    public async Task CreateAsync()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[create/cancelled]"));
            return;
        }

        Operation?.Invoke(this, new("[create/starting]"));

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await _queueClient.CreateIfNotExistsAsync(null, _cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[create/cancelled]"));
        }
        finally
        {
            _lock.Release();
        }

        Operation?.Invoke(this, new("[create/completed]"));
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

    public async Task DropAsync()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[drop/cancelled]"));
            return;
        }

        Operation?.Invoke(this, new("[drop/starting]"));

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await _queueClient.DeleteIfExistsAsync(_cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[drop/cancelled]"));
        }
        finally
        {
            _lock.Release();
        }

        Operation?.Invoke(this, new("[drop/completed]"));
    }

    public async Task PurgeAsync()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[purge/cancelled]"));
            return;
        }

        Operation?.Invoke(this, new("[purge/starting]"));

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await _queueClient.ClearMessagesAsync(_cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[purge/cancelled]"));
        }
        finally
        {
            _lock.Release();
        }

        Operation?.Invoke(this, new("[purge/completed]"));
    }

    public event EventHandler<MessageEnqueuedEventArgs>? MessageEnqueued;
    public event EventHandler<MessageAcknowledgedEventArgs>? MessageAcknowledged;
    public event EventHandler<MessageReleasedEventArgs>? MessageReleased;
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<OperationEventArgs>? Operation;

    public async ValueTask<bool> IsEmptyAsync()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[is-empty/cancelled]", true));
            return true;
        }

        Operation?.Invoke(this, new("[is-empty/starting]"));

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            var result = ((QueueProperties)await _queueClient.GetPropertiesAsync(_cancellationToken).ConfigureAwait(false)).ApproximateMessagesCount == 0;

            Operation?.Invoke(this, new("[is-empty]", result));

            return result;
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[is-empty/cancelled]", true));
        }
        finally
        {
            _lock.Release();
        }

        return true;
    }

    public async Task<ReceivedMessage?> GetMessageAsync()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[get-message/cancelled]"));
            return null;
        }

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (_receivedMessages.Count == 0)
            {
                Response<QueueMessage[]>? messages = null;

                try
                {
                    messages = await _queueClient.ReceiveMessagesAsync(_azureStorageQueueOptions.MaxMessages, _azureStorageQueueOptions.VisibilityTimeout, _cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Operation?.Invoke(this, new("[get-message/cancelled]"));
                }

                if (messages == null || messages.Value.Length == 0)
                {
                    return null;
                }

                foreach (var message in messages.Value)
                {
                    var acknowledgementToken = new AcknowledgementToken(message.MessageId, message.MessageText, message.PopReceipt);

                    if (_acknowledgementTokens.Remove(acknowledgementToken.MessageId))
                    {
                        Operation?.Invoke(this, new("[get-message/refreshed]", acknowledgementToken.MessageId));
                    }

                    _acknowledgementTokens.Add(acknowledgementToken.MessageId, acknowledgementToken);

                    _receivedMessages.Enqueue(new(new MemoryStream(Convert.FromBase64String(message.MessageText)), acknowledgementToken));
                }
            }

            var receivedMessage = _receivedMessages.Count > 0 ? _receivedMessages.Dequeue() : null;

            if (receivedMessage != null)
            {
                MessageReceived?.Invoke(this, new(receivedMessage));
            }

            return receivedMessage;
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[get-message/cancelled]"));
        }
        finally
        {
            _lock.Release();
        }

        return null;
    }

    public async Task ReleaseAsync(object acknowledgementToken)
    {
        if (Guard.AgainstNull(acknowledgementToken) is not AcknowledgementToken data)
        {
            return;
        }

        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[release/cancelled]"));
            return;
        }

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await _queueClient.SendMessageAsync(data.MessageText, _cancellationToken).ConfigureAwait(false);
            await _queueClient.DeleteMessageAsync(data.MessageId, data.PopReceipt, _cancellationToken).ConfigureAwait(false);

            MessageReleased?.Invoke(this, new(acknowledgementToken));
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[release/cancelled]"));
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

    public QueueUri Uri { get; }
    public bool IsStream => false;

    public async Task AcknowledgeAsync(object acknowledgementToken)
    {
        Guard.AgainstNull(acknowledgementToken);

        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[acknowledge/cancelled]"));
            return;
        }

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        if (!(acknowledgementToken is AcknowledgementToken data))
        {
            return;
        }

        try
        {
            await _queueClient.DeleteMessageAsync(data.MessageId, data.PopReceipt, _cancellationToken).ConfigureAwait(false);

            MessageAcknowledged?.Invoke(this, new(acknowledgementToken));
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[acknowledge/cancelled]"));
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

    public async Task EnqueueAsync(TransportMessage transportMessage, Stream stream)
    {
        Guard.AgainstNull(transportMessage);
        Guard.AgainstNull(stream);

        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[enqueue/cancelled]"));
            return;
        }

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await _queueClient.SendMessageAsync(Convert.ToBase64String(await stream.ToBytesAsync().ConfigureAwait(false)), null, _infiniteTimeToLive, _cancellationToken).ConfigureAwait(false);

            MessageEnqueued?.Invoke(this, new(transportMessage, stream));
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[enqueue/cancelled]"));
        }
        finally
        {
            _lock.Release();
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
}