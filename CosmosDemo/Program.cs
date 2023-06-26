using CosmosDemo;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
// initialize host
var app = CreateHost(args).Build();
using var scope = app.Services.CreateScope();
// resolve demo instances
var basicActionsDemo = scope.ServiceProvider.GetRequiredService<BasicActionsDemo>();
var transactionsDemo = scope.ServiceProvider.GetRequiredService<TransactionsDemo>();

await basicActionsDemo.Run();
await transactionsDemo.Run();

// hang on
Console.ReadLine();

IHostBuilder CreateHost(string[] strings)
{
    return Host.CreateDefaultBuilder(strings)
        .ConfigureAppConfiguration(config => { config.AddJsonFile("appsettings.dev.json").Build(); })
        .ConfigureServices((context, services) =>
        {
            services.AddTransient(
                _ => context.Configuration.GetSection(nameof(StorageSettings)).Get<StorageSettings>()!);
            services.AddScoped(isp =>
            {
                var storageSettings = isp.GetRequiredService<StorageSettings>();
                var options = new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                };
                return new CosmosClient(storageSettings.EndpointUrl, storageSettings.AuthorizationKey, options);
            });
            services.AddTransient<CosmosStorage>();
            services.AddTransient<BasicActionsDemo>();
            services.AddTransient<TransactionsDemo>();
        });
}