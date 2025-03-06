namespace ChatApp.WebApi.Model;

internal record RagConfig(string CollectionName, string AzureAISearchEndpoint, string AzureSearchApiKey, string AzureOpenAIEmbeddingsDeploymentName, string AzureOpenAIEmbeddingsEndpoint, string AzureOpenAIEmbeddingsApiKey);