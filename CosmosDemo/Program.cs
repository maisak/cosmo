using CosmosDemo;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
// initialize host
var app = CreateHost(args).Build();
using var scope = app.Services.CreateScope();
// resolve storage client
var storage = scope.ServiceProvider.GetRequiredService<CosmosStorage>();
// push new order to 'New' collection
var testOrder = new Order { Id = Guid.NewGuid().ToString(), ContractNumber = "number1" };
await storage.AddAsync(testOrder, Containers.New);
// get all orders from collection
var orders = await storage.GetAllOrdersAsync(Containers.New);
foreach (var order in orders)
{
    Console.WriteLine($"Order id: {order.Id} \t Number: {order.ContractNumber}");
}
// move to 'Processed' collection
var orderToMove = orders.First();
await storage.AddAsync(orderToMove, Containers.Processed);
await storage.DeleteFromNewOrdersAsync(orderToMove.Id);
// check if order exist in 'Processed' collection
var exist = await storage.OrderExistsInDatabaseAsync("number1");
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
        });
}