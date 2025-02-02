using System.Threading.Tasks;
using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AzureStorageQueues.Tests;

public class AzureStorageQueueOutboxFixture : OutboxFixture
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_be_able_to_use_outbox_async(bool isTransactionalEndpoint)
    {
        await TestOutboxSendingAsync(AzureStorageQueueConfiguration.GetServiceCollection(), "azuresq://azure/{0}", 3, isTransactionalEndpoint);
    }
}