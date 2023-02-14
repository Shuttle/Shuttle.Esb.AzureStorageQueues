using NUnit.Framework;
using Shuttle.Esb.Tests;
using System.Threading.Tasks;

namespace Shuttle.Esb.AzureStorageQueues.Tests
{
    public class AzureStorageQueueOutboxFixture : OutboxFixture
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_be_able_to_use_outbox(bool isTransactionalEndpoint)
        {
            await TestOutboxSending(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}", 3, isTransactionalEndpoint);
        }
    }
}