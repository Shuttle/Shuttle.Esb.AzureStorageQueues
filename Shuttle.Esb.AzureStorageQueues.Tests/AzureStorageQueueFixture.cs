using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AzureStorageQueues.Tests
{
    [TestFixture]
    public class AzureStorageQueueFixture : BasicQueueFixture
    {
        [Test]
        public void Should_be_able_to_perform_simple_enqueue_and_get_message()
        {
            TestSimpleEnqueueAndGetMessage(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}");
            TestSimpleEnqueueAndGetMessage(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}-transient");
        }

        [Test]
        public void Should_be_able_to_release_a_message()
        {
            TestReleaseMessage(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}");
        }

        [Test]
        public void Should_be_able_to_get_message_again_when_not_acknowledged_before_queue_is_disposed()
        {
            TestUnacknowledgedMessage(AzureFixture.GetServiceCollection(), "azuresq://azure/{0}");
        }
    }
}