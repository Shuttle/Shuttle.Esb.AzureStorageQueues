using Moq;
using Ninject;
using Shuttle.Core.Container;
using Shuttle.Core.Ninject;

namespace Shuttle.Esb.AzureMQ.Tests
{
    public static class AzureFixture
    {
        public static Esb.Tests.ComponentContainer GetComponentContainer()
        {
            var container = new NinjectComponentContainer(new StandardKernel());

            container.RegisterInstance(AzureStorageConfiguration());

            return new Esb.Tests.ComponentContainer(container, () => container);
        }

        private static IAzureStorageConfiguration AzureStorageConfiguration()
        {
            var mock = new Mock<IAzureStorageConfiguration>();

            mock.Setup(m => m.GetConnectionString(It.IsAny<string>())).Returns("UseDevelopmentStorage=true");

            return mock.Object;
        }
    }
}           
