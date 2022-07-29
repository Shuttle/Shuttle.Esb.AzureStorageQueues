using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AzureStorageQueues.Tests
{
    public class AzureStorageQueueDeferredMessageFixture : DeferredFixture
    {
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void Should_be_able_to_perform_full_processing(bool isTransactionalEndpoint)
        {
            TestDeferredProcessing(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}", isTransactionalEndpoint);
        }
    }
}