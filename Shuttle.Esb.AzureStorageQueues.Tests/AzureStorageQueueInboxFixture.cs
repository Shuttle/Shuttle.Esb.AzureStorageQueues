using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AzureStorageQueues.Tests
{
    public class AzureStorageQueueInboxFixture : InboxFixture
    {
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public async Task Should_be_able_handle_errors(bool hasErrorQueue, bool isTransactionalEndpoint)
        {
            await TestInboxError(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}", hasErrorQueue, isTransactionalEndpoint);
        }

        [TestCase(250, false)]
        [TestCase(250, true)]
        public async Task Should_be_able_to_process_messages_concurrently(int msToComplete, bool isTransactionalEndpoint)
        {
            await TestInboxConcurrency(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}", msToComplete, isTransactionalEndpoint);
        }

        [TestCase(100, true)]
        [TestCase(100, false)]
        public async Task Should_be_able_to_process_queue_timeously(int count, bool isTransactionalEndpoint)
        {
            await TestInboxThroughput(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}", 1000, count, 5, isTransactionalEndpoint);
        }

        [Test]
        public async Task Should_be_able_to_handle_a_deferred_message()
        {
            await TestInboxDeferred(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}");
        }
    }
}