using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AzureStorageQueues.Tests
{
    public class AzureStorageQueueDistributorFixture : DistributorFixture
    {
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void Should_be_able_to_distribute_messages(bool isTransactionalEndpoint)
        {
            TestDistributor(AzureFixture.GetServiceCollection(), 
                AzureFixture.GetServiceCollection(), @"azuremq://azure/{0}", isTransactionalEndpoint);
        }
    }
}