using Microsoft.Extensions.Options;

namespace Shuttle.Esb.AzureStorageQueues
{
    public class ConnectionStringOptionsValidator : IValidateOptions<ConnectionStringOptions>
    {
        public ValidateOptionsResult Validate(string name, ConnectionStringOptions options)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(options.Name))
            {
                return ValidateOptionsResult.Fail(Resources.ConnectionStringSettingsNameException);
            }

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                return ValidateOptionsResult.Fail(Resources.ConnectionStringSettingsConnectionStringException);
            }

            return ValidateOptionsResult.Success;
        }
    }
}