using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace CosmosDemo;

public class CosmosStorage
{
    private readonly CosmosClient _cosmosClient;
    private readonly StorageSettings _storageSettings;
    
    private Container NewOrdersContainer => _cosmosClient.GetContainer(_storageSettings.DatabaseId, _storageSettings.NewOrdersContainerId);
    private Container ProcessedOrdersContainer => _cosmosClient.GetContainer(_storageSettings.DatabaseId, _storageSettings.ProcessedOrdersContainerId);
    private Container FailedOrdersContainer => _cosmosClient.GetContainer(_storageSettings.DatabaseId, _storageSettings.FailedOrdersContainerId);

    public CosmosStorage(CosmosClient cosmosClient, StorageSettings storageSettings)
    {
        _cosmosClient = cosmosClient;
        _storageSettings = storageSettings;
    }
    
    public async Task AddAsync(Order order, Containers container)
    {
        _ = container switch
        {
            Containers.New          => await NewOrdersContainer.UpsertItemAsync(order),
            Containers.Processed    => await ProcessedOrdersContainer.UpsertItemAsync(order),
            Containers.Failed       => await FailedOrdersContainer.UpsertItemAsync(order),
            _                       => throw new Exception("Unsupported container type")
        };
    }
    
    public async Task<List<Order>> GetAllOrdersAsync(Containers containers)
    {
        List<Order> storedOrders;
        _ = containers switch
        {
            Containers.New          => storedOrders = await GetAllItemsAsync(NewOrdersContainer.GetItemLinqQueryable<Order>()),            
            Containers.Processed    => storedOrders = await GetAllItemsAsync(ProcessedOrdersContainer.GetItemLinqQueryable<Order>()),
            Containers.Failed       => storedOrders = await GetAllItemsAsync(FailedOrdersContainer.GetItemLinqQueryable<Order>()),
            _                       => throw new Exception("Unsupported container type")
        };

        return storedOrders;
    }

    public async Task<bool> OrderExistsInDatabaseAsync(string contractNumber)
    {
        var result = await GetSingleItemAsync(ProcessedOrdersContainer
            .GetItemLinqQueryable<Order>()
            .Where(x => x.ContractNumber == contractNumber));
        
        return result is not null;
    }

    public async Task DeleteFromNewOrdersAsync(string itemId) => 
        await NewOrdersContainer.DeleteItemAsync<Order>(itemId, new PartitionKey(itemId));
    
    public async Task<List<Order>> GetFailedOrdersAsync() => 
        await GetAllItemsAsync(FailedOrdersContainer.GetItemLinqQueryable<Order>());
        
    private static async Task<T?> GetSingleItemAsync<T>(IQueryable<T> queryable) where T : class
    {
        var feed = queryable.ToFeedIterator();

        while (feed.HasMoreResults)
        {
            foreach (var item in await feed.ReadNextAsync())
            {
                return item;
            }
        }

        return null;
    }
    
    private static async Task<List<T>> GetAllItemsAsync<T>(IQueryable<T> queryable) where T : class
    {
        var feed = queryable.ToFeedIterator();

        while (feed.HasMoreResults)
        {
            var items = (await feed.ReadNextAsync()).Resource;
            var itemsList = items.ToList();

            return itemsList;
        }

        return new List<T>();
    }
}