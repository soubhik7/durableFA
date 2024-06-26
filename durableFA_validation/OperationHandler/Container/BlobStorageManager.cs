using System;
using durableFA_validation.Config;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Identity;

namespace durableFA_validation.OperationHandler.Container
{
    public class BlobStorageManager : IBlobStorageManager
    {
        private readonly AppConfig _config;
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageManager(AppConfig config)
        {
            _config = config;
            //_blobServiceClient = new BlobServiceClient(new Uri($"https://{config.AccountName}.blob.core.windows.net"), new DefaultAzureCredential());
            _blobServiceClient = new BlobServiceClient(_config.StorageConnectionString);
        }
        public async Task StoreValidJsonInBlobStorage(string json, string fileName, string id, ILogger log)
        {
            var containerName = _config.ValidDataContainer;
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            string newFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{id}.json";

            var blobClient = containerClient.GetBlobClient(newFileName);
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            {
                await blobClient.UploadAsync(stream, true);
            }

            log.LogInformation($"JSON data stored in Blob Storage with file name: {fileName}");
        }
        public async Task StoreInValidJsonInBlobStorage(string json, string fileName, string id, ILogger log)
        {
            var containerName = _config.InValidDataContainer;
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            string newFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{id}.json";

            var blobClient = containerClient.GetBlobClient(newFileName);
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            {
                await blobClient.UploadAsync(stream, true);
            }

            log.LogInformation($"JSON data stored in Blob Storage with file name: {fileName}");
        }
        public async Task<List<string>> ListBlobNamesAsync(ILogger log)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_config.RawDataContainer);

                var blobNames = new List<string>();

                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    blobNames.Add(blobItem.Name);
                }

                return blobNames;
            }
            catch (Exception ex)
            {
                log.LogError($"Error listing blob names: {ex}");
                return new List<string>();
            }

        }
        public async Task<string> RetrieveBlobContentAsync(string blobName, ILogger log)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_config.RawDataContainer);
                var blobClient = containerClient.GetBlobClient(blobName);

                var response = await blobClient.DownloadAsync();

                using (var streamReader = new StreamReader(response.Value.Content))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error retrieving content for blob '{blobName}': {ex}");
                return string.Empty;
            }
        }
    }
}

