using Microsoft.Extensions.Options;

namespace Shuttle.Esb.AzureMQ
{
    public class ConnectionStringSettingsValidator : IValidateOptions<ConnectionStringSettings>
    {
        public ValidateOptionsResult Validate(string name, ConnectionStringSettings options)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(options.Name))
            {
                return ValidateOptionsResult.Fail(Resources.ConnectionStringSettingsNameException);
            }

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                return ValidateOptionsResult.Fail(Resources.ConnectionStringSettingsConnectionStringException);
            }

            return ValidateOptionsResult.Fail(name);
        }
    }
}