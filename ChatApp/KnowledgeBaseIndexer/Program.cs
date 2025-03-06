using System.Data;
using Azure;
using ChatApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

#pragma warning disable SKEXP0001

#pragma warning disable SKEXP0010

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Configure configuration and load the application configuration.
builder.Configuration.AddUserSecrets<Program>();
builder.Services.Configure<RagConfig>(builder.Configuration.GetSection(RagConfig.ConfigSectionName));
var appConfig = new ApplicationConfig(builder.Configuration);

CancellationTokenSource appShutdownCancellationTokenSource = new();
CancellationToken appShutdownCancellationToken = appShutdownCancellationTokenSource.Token;
builder.Services.AddKeyedSingleton("AppShutdown", appShutdownCancellationTokenSource);

var kernelBuilder = builder.Services.AddKernel();
kernelBuilder.AddAzureOpenAIChatCompletion(
        appConfig.AzureOpenAIConfig.ChatDeploymentName,
        appConfig.AzureOpenAIConfig.Endpoint,
        appConfig.AzureOpenAIConfig.ApiKey)
    .AddAzureOpenAITextEmbeddingGeneration(
        appConfig.AzureOpenAIEmbeddingsConfig.DeploymentName,
        appConfig.AzureOpenAIEmbeddingsConfig.Endpoint,
        apiKey: appConfig.AzureOpenAIEmbeddingsConfig.ApiKey
    )
    .AddAzureAISearchVectorStoreRecordCollection<KnowledgeBaseSnippet>(
        appConfig.RagConfig.CollectionName,
        new Uri(appConfig.AzureAISearchConfig.Endpoint),
        new AzureKeyCredential(appConfig.AzureAISearchConfig.ApiKey))
    .AddVectorStoreTextSearch<KnowledgeBaseSnippet>(
        new TextSearchStringMapper((result) => (result as KnowledgeBaseSnippet)!.Text!),
        new TextSearchResultMapper((result) =>
        {
            // Create a mapping from the Vector Store data type to the data type returned by the Text Search.
            // This text search will ultimately be used in a plugin and this TextSearchResult will be returned to the prompt template
            // when the plugin is invoked from the prompt template.
            var castResult = result as KnowledgeBaseSnippet;
            return new TextSearchResult(value: castResult!.Text!) { Name = castResult.ReferenceDescription, Link = castResult.ReferenceLink };
        }));

// Add the key generator and data loader to the dependency injection container.
builder.Services.AddSingleton<DataLoader, DataLoader>();

// Build and run the host.
using IHost host = builder.Build();

var dataLoader = host.Services.GetService<DataLoader>();
await dataLoader.LoadDataAsync(appShutdownCancellationToken);
