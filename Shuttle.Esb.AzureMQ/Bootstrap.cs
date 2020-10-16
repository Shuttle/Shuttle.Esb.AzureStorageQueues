using Shuttle.Core.Container;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureMQ
{
    public class Bootstrap : IComponentRegistryBootstrap
    {
        public void Register(IComponentRegistry registry)
        {
            Guard.AgainstNull(registry, nameof(registry));

            registry.AttemptRegister<IAzureStorageConfiguration, DefaultAzureStorageConfiguration>();
        }
    }
}