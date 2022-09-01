using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AzureStorageQueues.Tests
{
    public class AzureStorageQueueOutboxFixture : OutboxFixture
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Should_be_able_handle_errors(bool isTransactionalEndpoint)
        {
            TestOutboxSending(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}", 3, isTransactionalEndpoint);
        }
    }
}