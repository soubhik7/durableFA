using System;
using durableFA_validation.Config;
using durableFA_validation.OperationHandler.Container;
using durableFA_validation.OperationHandler.Table;
using durableFA_validation.ValidationCheck;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Schema;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace durableFA_validation
{
    public class TxDataPatchMain
    {
        private static bool isFirstRun = true;
        private readonly AppConfig _config;
        private readonly ITableStorageManager _tableStorageManager;
        private readonly IBlobStorageManager _blobStorageManager;
        private readonly JSchema _schema;
        private readonly HashSet<string> _processedBlobNames;

        public TxDataPatchMain(AppConfig config, ITableStorageManager tableStorageManager, IBlobStorageManager blobStorageManager, JSchema @object)
        {
            _config = config;
            _tableStorageManager = tableStorageManager;
            _blobStorageManager = blobStorageManager;
            //_schema = JSchema.Parse(ReturnSchema());
            _schema = JSchema.Parse(ReturnSchemaAsync().GetAwaiter().GetResult());
            _processedBlobNames = new HashSet<string>();
        }

        [FunctionName("tx-bulkupload-datavalidation_HttpStart")]
        public static async Task<IActionResult> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            if (isFirstRun)
            {
                log.LogInformation("Data patching validation FA is starting");
                isFirstRun = false;
            }
            try
            {
                string instanceId = await starter.StartNewAsync("Orchestrator_Function", null);

                log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

                return starter.CreateCheckStatusResponse(req, instanceId);
            }
            catch (Exception ex)
            {
                log.LogError($"Error starting orchestration: {ex}");
                return new StatusCodeResult(500);
            }
        }

        [FunctionName("Orchestrator_Function")]
        public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            try
            {
                var blobNames = await context.CallActivityAsync<List<string>>("GetBlobNames", null);

                if (blobNames == null || blobNames.Count == 0)
                {
                    log.LogWarning("No blobs found in the container.");
                    return;
                }

                var processingTasks = blobNames.Select(blobName => context.CallActivityAsync<Task>("ProcessBlobAsync", blobName));
                await Task.WhenAll(processingTasks);
            }
            catch (Exception ex)
            {
                log.LogError($"Error in orchestrator: {ex}");
            }
        }

        [FunctionName("GetBlobNames")]
        public async Task<List<string>> GetBlobNames([ActivityTrigger] string input, ILogger log)
        {
            try
            {
                var blobNames = await _blobStorageManager.ListBlobNamesAsync(log);
                return blobNames;
            }
            catch (Exception ex)
            {
                log.LogError($"Error getting blob names: {ex}");
                throw;
            }
        }

        [FunctionName("ProcessBlobAsync")]
        public async Task ProcessBlobAsync([ActivityTrigger] string blobName, ILogger log)
        {
            try
            {
                if (_processedBlobNames.Contains(blobName))
                {
                    log.LogInformation($"Blob '{blobName}' has already been processed. Skipping.");
                    return;
                }

                string blobContent = await _blobStorageManager.RetrieveBlobContentAsync(blobName, log);

                if (!string.IsNullOrEmpty(blobContent))
                {
                    var jsonProcessor = new IsValidation(_schema, _tableStorageManager, _blobStorageManager, _config);
                    await jsonProcessor.ProcessJsonArray(blobName, blobContent, log);
                }

                _processedBlobNames.Add(blobName);
            }
            catch (Exception ex)
            {
                log.LogError($"Error processing blob '{blobName}': {ex}");
                throw;
            }
        }

        public string GetFilePath(string relativePath)
        {
            var codeBaseUri = new Uri(Assembly.GetExecutingAssembly().Location);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUri.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);

            var projectDirectory = SearchForProjectDirectory(dirPath);
            return Path.Combine(projectDirectory, relativePath);
        }

        private string SearchForProjectDirectory(string currentDirectory)
        {
            var projectFiles = Directory.GetFiles(currentDirectory, "*.csproj");
            if (projectFiles.Any())
            {
                return currentDirectory;
            }
            var parentDirectory = Directory.GetParent(currentDirectory);
            return parentDirectory == null ?
                throw new Exception("Project directory not found.") : SearchForProjectDirectory(parentDirectory.FullName);
        }

        private async Task<string> ReturnSchemaAsync()
        {
            try
            {
                var path = GetFilePath("Templates\\json_Schema_global_Customer_Inbound_v7.4.1_r3.1.json");
                string templateFile = await LoadFileAsync(path);
                return templateFile;
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading schema.", ex);
            }
        }

        public async Task<string> LoadFileAsync(string path)
        {
            using (StreamReader r = new StreamReader(path))
            {
                string file = await r.ReadToEndAsync().ConfigureAwait(false);
                return file;
            }
        }
    }
}