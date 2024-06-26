using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace durableFA_validation.OperationHandler.Table
{
    public interface ITableStorageManager
    {
        Task InsertCustomerIntoTableStorage(string blobName, string correlationId, string status, string errorMessages, ILogger log);
    }
}

