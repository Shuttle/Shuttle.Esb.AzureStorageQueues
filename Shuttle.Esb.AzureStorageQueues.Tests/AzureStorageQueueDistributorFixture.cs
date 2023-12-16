using NUnit.Framework;
using Shuttle.Esb.Tests;
using System.Threading.Tasks;

namespace Shuttle.Esb.AzureStorageQueues.Tests
{
    public class AzureStorageQueueDistributorFixture : DistributorFixture
    {
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void Should_be_able_to_distribute_messages(bool isTransactionalEndpoint)
        {
            TestDistributor(AzureStorageQueueConfiguration.GetServiceCollection(), AzureStorageQueueConfiguration.GetServiceCollection(), @"azuresq://azure/{0}", isTransactionalEndpoint);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public async Task Should_be_able_to_distribute_messages_async(bool isTransactionalEndpoint)
        {
            await TestDistributorAsync(AzureStorageQueueConfiguration.GetServiceCollection(), AzureStorageQueueConfiguration.GetServiceCollection(), @"azuresq://azure/{0}", isTransactionalEndpoint);
        }
    }
}