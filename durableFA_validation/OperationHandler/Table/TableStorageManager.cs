using System;
using durableFA_validation.Config;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Identity;

namespace durableFA_validation.OperationHandler.Table
{
    public class TableStorageManager : ITableStorageManager
    {
        private readonly AppConfig _config;
        private readonly TableServiceClient _tableServiceClient;

        public TableStorageManager(AppConfig config)
        {
            _config = config;
            //_tableServiceClient = new TableServiceClient(new Uri($"https://{_config.AccountName}.table.core.windows.net"), new DefaultAzureCredential());
            _tableServiceClient = new TableServiceClient(config.StorageConnectionString);
        }
        public async Task InsertCustomerIntoTableStorage(string blobName, string correlationId, string status, string errorMessages, ILogger log)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient(_config.SummaryTable);
                await tableClient.CreateIfNotExistsAsync();

                string fileName = $"{Path.GetFileNameWithoutExtension(blobName)}_{correlationId}.json";
                var customerEntity = new Azure.Data.Tables.TableEntity
                {
                    PartitionKey = fileName,
                    RowKey = correlationId
                };
                customerEntity["Validation_Status"] = status;
                customerEntity["Validation_Error_Message"] = errorMessages;

                await tableClient.UpsertEntityAsync(customerEntity);

                log.LogInformation($"Customer data inserted/merged into Table Storage.");
            }
            catch (Exception ex)
            {
                log.LogError($"Error inserting customer into Table Storage: {ex}");
                throw;
            }
        }
    }
}

