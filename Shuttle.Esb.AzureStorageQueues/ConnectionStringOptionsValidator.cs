using Microsoft.Extensions.Options;

namespace Shuttle.Esb.AzureStorageQueues
{
    public class ConnectionStringOptionsValidator : IValidateOptions<AzureStorageQueueOptions>
    {
        public ValidateOptionsResult Validate(string name, AzureStorageQueueOptions options)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return ValidateOptionsResult.Fail(Esb.Resources.QueueConfigurationNameException);
            }

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                return ValidateOptionsResult.Fail(string.Format(Resources.QueueConfigurationItemException, name, nameof(options.ConnectionString)));
            }

            return ValidateOptionsResult.Success;
        }
    }
}