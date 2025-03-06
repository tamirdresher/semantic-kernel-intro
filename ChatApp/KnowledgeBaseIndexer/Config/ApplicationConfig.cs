using Microsoft.Extensions.Configuration;

internal sealed class ApplicationConfig
{
    private readonly AzureOpenAIConfig _azureOpenAIConfig;
    private readonly AzureOpenAIEmbeddingsConfig _azureOpenAIEmbeddingsConfig = new();
    private readonly RagConfig _ragConfig = new();
    private readonly AzureAISearchConfig _azureAISearchConfig = new();
    

    public ApplicationConfig(ConfigurationManager configurationManager)
    {
        this._azureOpenAIConfig = new();
        configurationManager
            .GetRequiredSection($"AIServices:{AzureOpenAIConfig.ConfigSectionName}")
            .Bind(this._azureOpenAIConfig);
        configurationManager
            .GetRequiredSection($"AIServices:{AzureOpenAIEmbeddingsConfig.ConfigSectionName}")
            .Bind(this._azureOpenAIEmbeddingsConfig);

        configurationManager
            .GetRequiredSection(RagConfig.ConfigSectionName)
            .Bind(this._ragConfig);
        configurationManager
            .GetRequiredSection($"VectorStores:{AzureAISearchConfig.ConfigSectionName}")
            .Bind(this._azureAISearchConfig);
      
    }

    public AzureOpenAIConfig AzureOpenAIConfig => this._azureOpenAIConfig;

    public AzureOpenAIEmbeddingsConfig AzureOpenAIEmbeddingsConfig => this._azureOpenAIEmbeddingsConfig;

    public AzureAISearchConfig AzureAISearchConfig => this._azureAISearchConfig;

    public RagConfig RagConfig => this._ragConfig;

}