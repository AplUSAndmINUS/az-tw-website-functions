using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Tables;

namespace StorageDiagnostics
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configuration - replace with your values or pass via arguments
            string storageAccountName = "aztwwebsitestorage"; // or use args[0]
            string clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"); // or use args[1]
            string containerName = "test-diagnostics";
            string tableName = "testdiagnostics";

            Console.WriteLine($"Storage Diagnostics Tool");
            Console.WriteLine($"========================");
            Console.WriteLine($"Storage Account: {storageAccountName}");
            Console.WriteLine($"Client ID: {clientId ?? "Not specified (using default credentials)"}");
            
            try
            {
                // Create DefaultAzureCredentialOptions with client ID if specified
                DefaultAzureCredentialOptions credOptions = null;
                if (!string.IsNullOrEmpty(clientId))
                {
                    credOptions = new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId };
                    Console.WriteLine("Using specified client ID for managed identity");
                }
                else
                {
                    credOptions = new DefaultAzureCredentialOptions();
                    Console.WriteLine("Using default credential chain");
                }
                
                var credential = new DefaultAzureCredential(credOptions);
                
                // Test blob storage
                Console.WriteLine("\nTesting Blob Storage...");
                var blobEndpoint = $"https://{storageAccountName}.blob.core.windows.net";
                Console.WriteLine($"Connecting to {blobEndpoint}");
                
                var blobClient = new BlobServiceClient(new Uri(blobEndpoint), credential);
                
                // List containers to test read access
                Console.WriteLine("Listing containers...");
                await foreach (var container in blobClient.GetBlobContainersAsync())
                {
                    Console.WriteLine($"- {container.Name}");
                }
                
                // Create a test container and blob to test write access
                Console.WriteLine($"Creating test container: {containerName}");
                var containerClient = blobClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();
                
                var blobName = $"test-{Guid.NewGuid()}.txt";
                Console.WriteLine($"Creating test blob: {blobName}");
                var testBlob = containerClient.GetBlobClient(blobName);
                
                var content = Encoding.UTF8.GetBytes($"Test content {DateTime.Now}");
                using (var stream = new MemoryStream(content))
                {
                    await testBlob.UploadAsync(stream, overwrite: true);
                }
                
                Console.WriteLine($"Successfully uploaded test blob");
                
                // Delete the test blob
                Console.WriteLine($"Deleting test blob");
                await testBlob.DeleteIfExistsAsync();
                
                // Test table storage
                Console.WriteLine("\nTesting Table Storage...");
                var tableEndpoint = $"https://{storageAccountName}.table.core.windows.net";
                Console.WriteLine($"Connecting to {tableEndpoint}");
                
                var tableClient = new TableServiceClient(new Uri(tableEndpoint), credential);
                
                // List tables to test read access
                Console.WriteLine("Listing tables...");
                await foreach (var table in tableClient.QueryAsync())
                {
                    Console.WriteLine($"- {table.Name}");
                }
                
                // Create a test table to test write access
                Console.WriteLine($"Creating test table: {tableName}");
                var testTableClient = tableClient.GetTableClient(tableName);
                await testTableClient.CreateIfNotExistsAsync();
                
                var entity = new TableEntity("test", Guid.NewGuid().ToString())
                {
                    { "Content", $"Test content {DateTime.Now}" }
                };
                
                Console.WriteLine("Adding test entity");
                await testTableClient.AddEntityAsync(entity);
                
                Console.WriteLine("Successfully added test entity");
                
                // Delete the test entity
                Console.WriteLine("Deleting test entity");
                await testTableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
                
                Console.WriteLine("\n✅ All storage tests passed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception type: {ex.InnerException.GetType().Name}");
                }
            }
        }
    }
}
