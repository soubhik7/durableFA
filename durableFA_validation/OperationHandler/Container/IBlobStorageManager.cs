using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace durableFA_validation.OperationHandler.Container
{
    public interface IBlobStorageManager
    {
        Task StoreValidJsonInBlobStorage(string json, string fileName, string id, ILogger log);
        Task StoreInValidJsonInBlobStorage(string json, string fileName, string id, ILogger log);
        Task<List<string>> ListBlobNamesAsync(ILogger log);
        Task<string> RetrieveBlobContentAsync(string blobName, ILogger log);

    }
}

