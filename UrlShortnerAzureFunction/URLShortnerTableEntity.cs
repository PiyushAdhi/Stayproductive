using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Azure;
using Azure.Data.Tables;

namespace AzureServerlessURLShortner_Helper
{
    // Define a strongly typed entity by implementing the ITableEntity interface.
    public class URLShortnerTableEntity: ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string ShortUrl { get; set; }
        public string LongUrl { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

    }
}
