using System;
using System.Collections.Generic;
using System.IO;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Streams;

namespace Shuttle.Esb.AzureMQ
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

        private readonly Dictionary<string, AcknowledgementToken> _acknowledgementTokens = new Dictionary<string,AcknowledgementToken>();
        private readonly Queue<ReceivedMessage> _receivedMessages = new Queue<ReceivedMessage>();
        private readonly object _lock = new object();

        private readonly QueueClient _queueClient;
        private readonly int _maxMessages;

        public AzureStorageQueue(Uri uri, IOptionsMonitor<ConnectionStringOptions> connectionStringOptions)
        {
            Guard.AgainstNull(uri, nameof(uri));
            Guard.AgainstNull(connectionStringOptions, nameof(connectionStringOptions));

            Uri = uri;

            var parser = new AzureStorageQueueUriParser(uri);

            var connectionStringSettings = connectionStringOptions.Get(parser.StorageConnectionStringName);

            if (connectionStringSettings == null)
            {
                throw new InvalidOperationException(string.Format(Resources.UnknownConnectionStringException, parser.StorageConnectionStringName));
            }

            _queueClient = new QueueClient(connectionStringSettings.ConnectionString, parser.QueueName);
            _maxMessages = parser.MaxMessages;
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
                _queueClient.SendMessage(Convert.ToBase64String(stream.ToBytes()));
            }
        }

        public ReceivedMessage GetMessage()
        {
            lock (_lock)
            {
                if (_receivedMessages.Count == 0)
                {
                    var messages = _queueClient.ReceiveMessages(_maxMessages);

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

                _queueClient.DeleteMessage(data.MessageId, data.PopReceipt);
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
                _queueClient.SendMessage(data.MessageText);
                _queueClient.DeleteMessage(data.MessageId, data.PopReceipt);

                if (_acknowledgementTokens.ContainsKey(data.MessageId))
                {
                    _acknowledgementTokens.Remove(data.MessageId);
                }
            }
        }

        public Uri Uri { get; }
        public bool IsStream => false;

        public void Create()
        {
            lock (_lock)
            {
                _queueClient.CreateIfNotExists();
            }
        }

        public void Drop()
        {
            lock (_lock)
            {
                _queueClient.DeleteIfExists();
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
                _queueClient.ClearMessages();
            }
        }
    }
}
