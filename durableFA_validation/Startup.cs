using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using durableFA_validation.OperationHandler.Container;
using durableFA_validation.OperationHandler.Table;
using Newtonsoft.Json.Schema;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using durableFA_validation.Config;
using System.Linq;

[assembly: FunctionsStartup(typeof(durableFA_validation.Startup))]

namespace durableFA_validation
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            ConfigureServices(builder.Services);

            builder.Services.AddSingleton<ITableStorageManager, TableStorageManager>();
            builder.Services.AddSingleton<IBlobStorageManager, BlobStorageManager>();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton((s) =>
            {
                return new AppConfig();
            });
            services.AddSingleton<JSchema>((s) =>
            {
                var schemaFilePath = GetSchemaFilePath();
                var schemaContent = File.ReadAllText(schemaFilePath);
                return JSchema.Parse(schemaContent);
            });
        }

        // Schema path searching
        private static async Task<JSchema> LoadSchemaAsync()
        {
            var schemaFilePath = GetSchemaFilePath();
            var schemaContent = await File.ReadAllTextAsync(schemaFilePath);
            return JSchema.Parse(schemaContent);
        }

        private static string GetSchemaFilePath()
        {
            var codeBaseUri = new Uri(Assembly.GetExecutingAssembly().Location);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUri.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);

            var projectDirectory = SearchForProjectDirectory(dirPath);
            return Path.Combine(projectDirectory, "Templates", "json_Schema_global_Customer_Inbound_v7.4.1_r3.1.json");
        }

        private static string SearchForProjectDirectory(string currentDirectory)
        {
            while (true)
            {
                var projectFiles = Directory.GetFiles(currentDirectory, "*.csproj");
                if (projectFiles.Any())
                {
                    return currentDirectory;
                }
                var parentDirectory = Directory.GetParent(currentDirectory);
                if (parentDirectory == null)
                {
                    throw new Exception("Project directory not found.");
                }
                currentDirectory = parentDirectory.FullName;
            }
        }
    }
}