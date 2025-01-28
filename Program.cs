using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using PizzaLover.Handler;
using Umbraco.Cms.Core.Notifications;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

var keyVaultName = "PizzaLovervault";
var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
var secretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());

builder.Configuration.AddAzureKeyVault(
    keyVaultUri,
    new DefaultAzureCredential());

try
{
    // Hämta SQL Server-anslutningssträngen från Key Vault
    var databaseSecret = await secretClient.GetSecretAsync("sqlServer");
    builder.Configuration["ConnectionStrings:umbracoDbDSN"] = databaseSecret.Value.Value;

    // Hämta Blob Storage-anslutningssträngen från Key Vault
    var blobStorageSecret = await secretClient.GetSecretAsync("PizzaLoverBlob");
    builder.Configuration["Umbraco:Storage:AzureBlob:Media:ConnectionString"] = blobStorageSecret.Value.Value;

    // Lägg till ContainerName om det behövs
    builder.Configuration["Umbraco:Storage:AzureBlob:Media:ContainerName"] = "pizzalovercontainer";
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to retrieve secrets from Key Vault: {ex.Message}");
    throw;
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

builder.Services.AddSingleton<MediaUploadedHandler>();

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .AddNotificationHandler<MediaSavedNotification, MediaUploadedHandler>()
    .AddAzureBlobMediaFileSystem()

    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();


app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
