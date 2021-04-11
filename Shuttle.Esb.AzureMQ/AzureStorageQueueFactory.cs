using System;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AzureMQ
{
    public class AzureStorageQueueFactory : IQueueFactory
    {
        private readonly IAzureStorageConfiguration _configuration;
        public string Scheme => AzureStorageQueueUriParser.Scheme;

        public AzureStorageQueueFactory(IAzureStorageConfiguration configuration)
        {
            Guard.AgainstNull(configuration, nameof(configuration));

            _configuration = configuration;
        }

        public IQueue Create(Uri uri)
        {
            Guard.AgainstNull(uri, "uri");

            return new AzureStorageQueue(uri, _configuration);
        }
                                                
        public bool CanCreate(Uri uri)
        {                                   
            Guard.AgainstNull(uri, "uri");

            return Scheme.Equals(uri.Scheme, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}