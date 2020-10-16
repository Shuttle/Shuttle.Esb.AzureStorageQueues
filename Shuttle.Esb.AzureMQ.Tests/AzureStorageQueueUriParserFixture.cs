using System;
using NUnit.Framework;

namespace Shuttle.Esb.AzureMQ.Tests
{
    [TestFixture]
    public class AzureStorageQueueUriParserFixture
    {
        [Test]
        public void Should_be_able_to_parse_all_parameters()
        {
            var parser =
                new AzureStorageQueueUriParser(
                    new Uri("azuremq://connection-name/queue-name"));

            Assert.AreEqual("connection-name", parser.StorageConnectionStringName);
            Assert.AreEqual("queue-name", parser.QueueName);
            Assert.AreEqual(1, parser.MaxMessages);

            parser =
                new AzureStorageQueueUriParser(
                    new Uri("azuremq://connection-name/queue-name?maxMessages=15"));

            Assert.AreEqual("connection-name", parser.StorageConnectionStringName);
            Assert.AreEqual("queue-name", parser.QueueName);
            Assert.AreEqual(15, parser.MaxMessages);
        }
    }
}