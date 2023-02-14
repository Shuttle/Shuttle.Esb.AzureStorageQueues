using System.Threading.Tasks;
using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AzureStorageQueues.Tests
{
    [TestFixture]
    public class AzureStorageQueueFixture : BasicQueueFixture
    {
        [Test]
        public async Task Should_be_able_to_perform_simple_enqueue_and_get_message()
        {
            await TestSimpleEnqueueAndGetMessage(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}");
            await TestSimpleEnqueueAndGetMessage(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}-transient");
        }

        [Test]
        public async Task Should_be_able_to_release_a_message()
        {
            await TestReleaseMessage(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}");
        }

        [Test]
        public async Task Should_be_able_to_get_message_again_when_not_acknowledged_before_queue_is_disposed()
        {
            await TestUnacknowledgedMessage(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}");
        }
    }
}