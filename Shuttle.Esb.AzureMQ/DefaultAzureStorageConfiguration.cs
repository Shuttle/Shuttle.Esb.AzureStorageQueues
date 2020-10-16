using Shuttle.Core.Configuration;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureMQ
{
    public class DefaultAzureStorageConfiguration : IAzureStorageConfiguration
    {
        public string GetConnectionString(string name)
        {
            Guard.AgainstNullOrEmptyString(name, nameof(name));

            return ConfigurationItem<string>.ReadSetting(name).GetValue();
        }
    }
}