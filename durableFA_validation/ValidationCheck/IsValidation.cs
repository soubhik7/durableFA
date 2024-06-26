using System;
using durableFA_validation.Config;
using durableFA_validation.Helper;
using durableFA_validation.OperationHandler.Container;
using durableFA_validation.OperationHandler.Table;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Schema;
using System.Linq;

namespace durableFA_validation.ValidationCheck
{
    public class IsValidation
    {
        private readonly AppConfig _config;
        private readonly JSchema _schema;
        private readonly ITableStorageManager _tableStorageManager;
        private readonly IBlobStorageManager _blobStorageManager;
        private HashSet<string> processedCorrelationIds = new HashSet<string>();

        public IsValidation(JSchema schema, ITableStorageManager tableStorageManager, IBlobStorageManager blobStorageManager, AppConfig config)
        {
            _schema = schema;
            _tableStorageManager = tableStorageManager;
            _blobStorageManager = blobStorageManager;
            _config = config;
            //License.RegisterLicense(_config.NewtonsoftLicenseKey); // Open during actual bulk run - 1000 /hr limit
        }

        public async Task ProcessJsonArray(string blobName, string jsonArrayString, ILogger log)
        {
            try
            {
                JArray jsonArray = JArray.Parse(jsonArrayString);

                foreach (var jsonObject in jsonArray.OfType<JObject>())
                {
                    string correlationId = GetCorrelationId(jsonObject, log);
                    if (!string.IsNullOrEmpty(correlationId) && processedCorrelationIds.Contains(correlationId))
                    {
                        log.LogInformation($"Skipping processing for duplicate correlationId: {correlationId}");
                        continue;
                    }
                    IList<string> validationErrors = new List<string>();
                    bool isValid = jsonObject.IsValid(_schema, out validationErrors);

                    if (!isValid)
                    {
                        await InsertData(blobName, jsonObject, validationErrors, log);

                    }
                    else
                    {
                        await InsertData(blobName, jsonObject, validationErrors, log);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error processing Json array in blob '{blobName}': {ex}");
            }

        }

        private async Task InsertData(string blobName, JObject jsonObject, IList<string> validationErrors, ILogger log)
        {
            try
            {
                string correlationId = GetCorrelationId(jsonObject, log);
                processedCorrelationIds.Add(correlationId);


                string storageConnectionString = _config.StorageConnectionString;
                string validationStatus = jsonObject.IsValid(_schema) ? "Valid" : "Invalid";

                if (validationStatus == "Valid")
                {
                    string errorMessages = string.Join(", ", "NA");
                    await _tableStorageManager.InsertCustomerIntoTableStorage(blobName, correlationId, validationStatus, errorMessages, log);
                    await StoreValidJsonData(jsonObject.ToString(), blobName, correlationId, log);
                }
                else
                {
                    string errorMessages = string.Join(", ", validationErrors);
                    await _tableStorageManager.InsertCustomerIntoTableStorage(blobName, correlationId, validationStatus, errorMessages, log);
                    await StoreInValidJsonData(jsonObject.ToString(), blobName, correlationId, log);
                }
                log.LogInformation($"Customer data inserted/merged into Table Storage with validation status: {validationStatus}");
            }
            catch (Exception ex)
            {
                log.LogError($"Error inserting data into Table storage for blob '{blobName}': {ex}");
            }
        }

        private async Task StoreValidJsonData(string json, string blobName, string id, ILogger log)
        {
            await _blobStorageManager.StoreValidJsonInBlobStorage(json, blobName, id, log);
        }
        private async Task StoreInValidJsonData(string json, string blobName, string id, ILogger log)
        {
            await _blobStorageManager.StoreInValidJsonInBlobStorage(json, blobName, id, log);
        }
        private string GetCorrelationId(JObject jsonObject, ILogger log)
        {
            try
            {
                var customer = jsonObject.ToObject<TxDataTracker>();
                return customer?.customerTechnicalHeader?.correlationId;
            }
            catch (Exception ex)
            {
                log.LogError($"Error extracting correlationId: {ex}");
                return null;
            }
        }
    }
}

