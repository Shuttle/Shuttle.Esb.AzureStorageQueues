using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AzureMQ.Tests
{
    public class AzureStorageQueuePipelineExceptionHandlingFixture : PipelineExceptionFixture
    {
        [Test]
        public void Should_be_able_to_handle_exceptions_in_receive_stage_of_receive_pipeline()
        {
            TestExceptionHandling(AzureFixture.GetComponentContainer(), "azuremq://azure/{0}");
        }
    }
}