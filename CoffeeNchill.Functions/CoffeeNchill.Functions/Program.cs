using Azure.Data.Tables;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        IConfiguration configuration = context.Configuration;

        // Connection string for Azurite / Azure Storage. Read once and reused for
        // both Table Storage (MenuItems) and File Storage (staff-docs).
        string storageConnectionString = configuration["AzureWebJobsStorage"]
            ?? "UseDevelopmentStorage=true";

        string staffDocsShareName = configuration["STAFF_DOCS_SHARE_NAME"] ?? "staff-docs";

        // Register a TableServiceClient and make sure the MenuItems table exists on startup.
        services.AddSingleton(sp =>
        {
            var tableServiceClient = new TableServiceClient(storageConnectionString);
            tableServiceClient.CreateTableIfNotExists("MenuItems");
            return tableServiceClient;
        });

        // Register a ShareClient for the staff-docs file share and make sure it exists.
        services.AddSingleton(sp =>
        {
            var shareServiceClient = new ShareServiceClient(storageConnectionString);
            ShareClient shareClient = shareServiceClient.GetShareClient(staffDocsShareName);
            shareClient.CreateIfNotExists();
            return shareClient;
        });

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
