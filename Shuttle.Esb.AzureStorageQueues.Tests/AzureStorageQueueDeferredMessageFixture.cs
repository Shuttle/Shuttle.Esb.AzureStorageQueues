using System.Threading.Tasks;
using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AzureStorageQueues.Tests
{
    public class AzureStorageQueueDeferredMessageFixture : DeferredFixture
    {
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public async Task Should_be_able_to_perform_full_processing(bool isTransactionalEndpoint)
        {
            await TestDeferredProcessing(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}", isTransactionalEndpoint);
        }
    }
}