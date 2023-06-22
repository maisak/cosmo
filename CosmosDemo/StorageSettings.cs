using JetBrains.Annotations;

namespace CosmosDemo;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class StorageSettings
{
    public string EndpointUrl { get; set; } = null!;
    public string AuthorizationKey { get; set; } = null!;
    public string DatabaseId { get; set; } = null!;
    public string NewOrdersContainerId { get; set; } = null!;
    public string ProcessedOrdersContainerId { get; set; } = null!;
    public string FailedOrdersContainerId { get; set; } = null!;
}