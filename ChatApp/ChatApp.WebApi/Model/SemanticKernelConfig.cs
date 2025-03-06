using ChatApp.WebApi.Interfaces;

namespace ChatApp.WebApi.Model;

internal struct SemanticKernelConfig
{
    internal AzureOpenAIConfig AzureOpenAIConfig { get; private init; }
    internal RagConfig RagConfig { get; private init; }

    internal static async Task<SemanticKernelConfig> CreateAsync(ISecretStore secretStore, CancellationToken cancellationToken)
    {
        var azureDeployment = await secretStore.GetSecretAsync("AzureDeployment", cancellationToken);
        var azureEndpoint = await secretStore.GetSecretAsync("AzureEndpoint", cancellationToken);

        string collectionName = await secretStore.GetSecretAsync("AzureSearchCollectionName", cancellationToken);
        string azureAISearchEndpoint = await secretStore.GetSecretAsync("AzureSearchEndpoint", cancellationToken);
        string apiKey = await secretStore.GetSecretAsync("AzureSearchApiKey", cancellationToken);

        string azureOpenAIEmbeddingsApiKey = await secretStore.GetSecretAsync("AzureOpenAIEmbeddingsApiKey", cancellationToken);
        string azureOpenAIEmbeddingsEndpoint = await secretStore.GetSecretAsync("AzureOpenAIEmbeddingsEndpoint", cancellationToken);
        string azureOpenAIEmbeddingsDeploymentName = await secretStore.GetSecretAsync("AzureOpenAIEmbeddingsDeploymentName", cancellationToken);

        return new SemanticKernelConfig
        {
            AzureOpenAIConfig = new AzureOpenAIConfig(azureDeployment, azureEndpoint),
            RagConfig = new RagConfig(collectionName, azureAISearchEndpoint, apiKey, azureOpenAIEmbeddingsDeploymentName, azureOpenAIEmbeddingsEndpoint, azureOpenAIEmbeddingsApiKey)
        };
    }
}