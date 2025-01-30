using Azure.Storage.Blobs;
using System.Text.Json;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace PizzaLover.Handler
{
    public class MediaUploadedHandler : INotificationHandler<MediaSavedNotification>
    {
        private readonly string _connectionString;
        private readonly string _containerName;
        public MediaUploadedHandler(IConfiguration configuration)
        {
            // Hämta värden från konfigurationen
            _connectionString = configuration["Umbraco:Storage:AzureBlob:Media:ConnectionString"]!;
            _containerName = configuration["Umbraco:Storage:AzureBlob:Media:ContainerName"]!;
        }

        public void Handle(MediaSavedNotification notification)
        {
            foreach (var media in notification.SavedEntities)
            {
                if (media.ContentType.Alias == "Image") // Kontrollera att det är en bild
                {
                    // Hämta AltText från Umbraco
                    var altText = media.GetValue<string>("altText") ?? string.Empty;
                    var filePathJson = media.GetValue<string>("umbracoFile");
                    altText = System.Text.Encoding.UTF8.GetString(System.Text.Encoding.UTF8.GetBytes(altText));


                    // Parsar JSON för att hämta "src"-värdet
                    string blobName;
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        };
                        var filePathData = JsonSerializer.Deserialize<JsonDocument>(filePathJson, options);
                        blobName = filePathData.RootElement.GetProperty("src").GetString()?.TrimStart('/');
                    }
                    catch (Exception ex)
                    {
                        // Logga om JSON inte kan parsas
                        Console.WriteLine($"Error parsing filePath JSON: {ex.Message}");
                        continue;
                    }

                    if (string.IsNullOrEmpty(blobName))
                    {
                        Console.WriteLine("BlobName could not be extracted from JSON.");
                        continue;
                    }

                    // Lägg till metadata i Azure Blob Storage
                    var blobClient = new BlobServiceClient(_connectionString)
                        .GetBlobContainerClient(_containerName)
                        .GetBlobClient(blobName);

                    var metadata = new Dictionary<string, string>
                {
                    //{ "altText", altText ?? string.Empty }
                    { "altText", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(altText)) }
                };

                    //Kontrollera om blobben existerar och sätt metadata
                    if (blobClient.Exists())
                    {
                        try
                        {
                            blobClient.SetMetadata(metadata);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error setting metadata: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Blob does not exist.");
                    }
                }
            }
        }
    }
}

