namespace CosmosDemo;

public class BasicActionsDemo
{
    private readonly CosmosStorage _cosmosStorage;

    public BasicActionsDemo(CosmosStorage cosmosStorage)
    {
        _cosmosStorage = cosmosStorage;
    }
    public async Task Run()
    {
        // push new order to 'New' collection
        var testOrder = new Order { Id = Guid.NewGuid().ToString(), ContractNumber = "number1" };
        await _cosmosStorage.AddAsync(testOrder, Containers.New);
        // get all orders from collection
        var orders = await _cosmosStorage.GetAllOrdersAsync(Containers.New);
        foreach (var order in orders)
        {
            Console.WriteLine($"Order id: {order.Id} \t Number: {order.ContractNumber}");
        }

        // move to 'Processed' collection
        var orderToMove = orders.First();
        await _cosmosStorage.AddAsync(orderToMove, Containers.Processed);
        await _cosmosStorage.DeleteFromNewOrdersAsync(orderToMove.Id);
        // check if order exist in 'Processed' collection
        var exist = await _cosmosStorage.OrderExistsInDatabaseAsync("number1");
    }
}