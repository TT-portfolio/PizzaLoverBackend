using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

var keyVaultName = "PizzaLovervault";
var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");

builder.Configuration.AddAzureKeyVault(
    keyVaultUri,
    new DefaultAzureCredential());

var secretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());
var databaseSecret = secretClient.GetSecret("sqlServer").Value;

// Sätt anslutningssträngen i konfigurationen
builder.Configuration["ConnectionStrings:umbracoDbDSN"] = databaseSecret.Value;

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
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
