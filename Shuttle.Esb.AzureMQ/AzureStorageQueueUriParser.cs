using System;
using System.Linq;
using System.Xml.XPath;
using Shuttle.Core.Contract;
using Shuttle.Core.Uris;

namespace Shuttle.Esb.AzureMQ
{
    public class AzureStorageQueueUriParser
    {
        internal const string Scheme = "azuremq";

        public AzureStorageQueueUriParser(Uri uri)
        {
            Guard.AgainstNull(uri, "uri");

            if (!uri.Scheme.Equals(Scheme, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidSchemeException(Scheme, uri.ToString());
            }

            if (uri.LocalPath == "/" || uri.Segments.Length != 2)
            {
                throw new UriFormatException(string.Format(Esb.Resources.UriFormatException,
                    $"{Scheme}://{{storage-connection-string-name}}/{{queue-name}}",  uri));
            }

            Uri = uri;

            StorageConnectionStringName = Uri.Host;
            QueueName = Uri.Segments[1];

            var queryString = new QueryString(uri);

            SetMaxMessages(queryString);
        }

        public Uri Uri { get; }
        public string StorageConnectionStringName { get; }
        public string QueueName { get; }
        public int MaxMessages { get; private set; }

        private void SetMaxMessages(QueryString queryString)
        {
            MaxMessages = 1;

            var parameter = queryString["maxMessages"];

            if (parameter == null)
            {
                return;
            }

            if (ushort.TryParse(parameter, out var result))
            {
                if (result < 1)
                {
                    result = 1;
                }

                if (result > 32)
                {
                    result = 32;
                }

                MaxMessages = result;
            }
        }
    }
}