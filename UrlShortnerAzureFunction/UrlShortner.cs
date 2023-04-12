using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Data.Tables;
using System.Web.Http;
using AzureServerlessURLShortner_Helper;
using System.Reflection.Metadata;
using System.Linq;
using System.Net;
using Azure;

namespace UrlShortnerAzureFunction
{
    public static class UrlShortner
    {
        [FunctionName("UrlShortner")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("UrlShortner function: started");

            try
            {
                if (req.Method == HttpMethods.Post)
                {
                    // Generate unique partition and row keys  
                    string partitionKey = Guid.NewGuid().ToString();
                    string rowKey = Guid.NewGuid().ToString();

                    // Read the long URL from the request body  
                    string longUrl = String.Empty ;
                    using (StreamReader streamReader = new StreamReader(req.Body))
                    {
                        longUrl = streamReader.ReadToEnd();
                    }

                    // Generate the short URL  
                    string functionName = context.FunctionName;
                    string shortUrl = $"{req.Scheme}://{req.Host}{req.Path}?PartitionKey={partitionKey}&RowKey={rowKey}";

                    // Save the short URL and long URL to Table Storage  
                    string tableStorageAccountConnection = Environment.GetEnvironmentVariable("TableStorageAccountConnection");
                    string tableName = Environment.GetEnvironmentVariable("TableName");
                    TableClient tableClient = new TableClient(tableStorageAccountConnection, tableName);
                    tableClient.CreateIfNotExists();
                    URLShortnerTableEntity shortnerTableEntity = new URLShortnerTableEntity()
                    {
                        ShortUrl = shortUrl,
                        LongUrl = longUrl,
                        PartitionKey = partitionKey,
                        RowKey = rowKey
                    };
                    tableClient.AddEntity(shortnerTableEntity);

                    log.LogInformation("UrlShortner function: post ended");

                    return new OkObjectResult(shortUrl);
                }
                else if (req.Method == HttpMethods.Get)
                {
                    // Retrieve the short URL from the query string  
                    string partitionKey = req.Query["PartitionKey"];
                    string rowKey = req.Query["RowKey"];

                    // Retrieve the corresponding long URL from Table Storage  
                    string tableStorageAccountConnection = Environment.GetEnvironmentVariable("TableStorageAccountConnection");
                    string tableName = Environment.GetEnvironmentVariable("TableName");
                    TableClient tableClient = new TableClient(tableStorageAccountConnection, tableName);
                    var queryresults = tableClient.Query<URLShortnerTableEntity>(filter: $"PartitionKey eq '{partitionKey}' and RowKey eq '{rowKey}'").FirstOrDefault();
                    if (queryresults == null)
                    {
                        log.LogInformation("UrlShortner function: get ended (short URL not found)");
                        return new NotFoundResult();
                    }
                    else
                    {
                        log.LogInformation("UrlShortner function: get ended");
                        return new RedirectResult(queryresults.LongUrl);
                    }
                }
                else
                {
                    log.LogInformation("UrlShortner function: Exception (unsupported HTTP method)");
                    return new BadRequestObjectResult("Unsupported HTTP method");
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"UrlShortner function: Exception ({ex.Message})");
                return new JsonResult(ex.Message)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }
    }
}
