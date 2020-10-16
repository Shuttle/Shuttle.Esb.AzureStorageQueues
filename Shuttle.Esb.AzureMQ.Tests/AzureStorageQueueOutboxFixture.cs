using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AzureMQ.Tests
{
    public class AzureStorageQueueOutboxFixture : OutboxFixture
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Should_be_able_handle_errors(bool isTransactionalEndpoint)
        {
            TestOutboxSending(AzureFixture.GetComponentContainer(), "azuremq://azure/{0}", isTransactionalEndpoint);
        }
    }
}