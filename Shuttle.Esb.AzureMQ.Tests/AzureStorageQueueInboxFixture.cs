using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AzureMQ.Tests
{
    public class AzureStorageQueueInboxFixture : InboxFixture
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Should_be_able_handle_errors(bool isTransactionalEndpoint)
        {
            TestInboxError(AzureFixture.GetComponentContainer(), "azuremq://azure/{0}", isTransactionalEndpoint);
        }

        [TestCase(250, false)]
        [TestCase(250, true)]
        public void Should_be_able_to_process_messages_concurrently(int msToComplete, bool isTransactionalEndpoint)
        {
            TestInboxConcurrency(AzureFixture.GetComponentContainer(), "azuremq://azure/{0}", msToComplete, isTransactionalEndpoint);
        }

        [TestCase(100, true)]
        [TestCase(100, false)]
        public void Should_be_able_to_process_queue_timeously(int count, bool isTransactionalEndpoint)
        {
            TestInboxThroughput(AzureFixture.GetComponentContainer(), "azuremq://azure/{0}", 1000, count, isTransactionalEndpoint);
        }

        [Test]
        public void Should_be_able_to_handle_a_deferred_message()
        {
            TestInboxDeferred(AzureFixture.GetComponentContainer(), "azuremq://azure/{0}");
        }

        [Test]
        public void Should_be_able_to_expire_a_message()
        {
            TestInboxExpiry(AzureFixture.GetComponentContainer(), "azuremq://azure/{0}");
        }
    }
}