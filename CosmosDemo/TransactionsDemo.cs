using JetBrains.Annotations;
using Microsoft.Azure.Cosmos;

namespace CosmosDemo;

public class TransactionsDemo
{
    private readonly CosmosClient _client;
    private readonly StorageSettings _settings;

    public TransactionsDemo(CosmosClient client, StorageSettings settings)
    {
        _client = client;
        _settings = settings;
    }
    
    public async Task Run()
    {
        // create container for user and their product links
        var containerProperties = new ContainerProperties
        {
            Id = "UserAndProductLinks",
            PartitionKeyPath = "/userId"
        };
        var database = _client.GetDatabase(_settings.DatabaseId);
        var container = (await database.CreateContainerAsync(containerProperties)).Container;
        // user and a product links to create
        const string partitionKey = "user1";
        var user = new User { Id = "u1", UserId = partitionKey, Name = "Anthony" };
        var userToProduct1 = new UserProduct { Id = "p1", UserId = partitionKey, ProductName = "Jetbrains Rider" };
        var userToProduct2 = new UserProduct { Id = "p2", UserId = partitionKey, ProductName = "Jetbrains dotCover" };
        // successful batch
        var successfulBatch = container.CreateTransactionalBatch(new PartitionKey(partitionKey))
            .CreateItem(user)
            .CreateItem(userToProduct1)
            .CreateItem(userToProduct2);
        await ExecuteBatch(successfulBatch);
        // failed batch when updating the user's name and trying to add a new product link
        var fetchedUser = (await container.ReadItemAsync<User>("u1", new PartitionKey(partitionKey))).Resource;
        var failingProductLink = new UserProduct { Id = "p2", UserId = partitionKey, ProductName = "Jetbrains dotMemory" };
        fetchedUser.Name = "Anton";
        var failedBatch = container.CreateTransactionalBatch(new PartitionKey(partitionKey))
            .ReplaceItem(fetchedUser.Id, fetchedUser)
            .CreateItem(failingProductLink);
        await ExecuteBatch(failedBatch);
    }
    
    private static async Task ExecuteBatch(TransactionalBatch batch)
    {
        var batchResponse = await batch.ExecuteAsync();

        using (batchResponse)
        {
            if (batchResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Transactional batch succeeded");
                for (var i = 0; i < batchResponse.Count; i++)
                {
                    var result = batchResponse.GetOperationResultAtIndex<dynamic>(i);
                    Console.WriteLine($"Document {i + 1}:");
                    Console.WriteLine(result.Resource);
                }
            }
            else
            {
                Console.WriteLine("Transactional batch failed");
                for (var i = 0; i < batchResponse.Count; i++)
                {
                    var result = batchResponse.GetOperationResultAtIndex<dynamic>(i);
                    Console.WriteLine($"Document {i + 1}: {result.StatusCode}");
                }
            }
        }
    }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class User
{
    public string Id { get; init; } = null!;
    public string UserId { get; set; } = null!;
    public string Name { get; set; } = null!;
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class UserProduct
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string ProductName { get; set; } = null!;
}