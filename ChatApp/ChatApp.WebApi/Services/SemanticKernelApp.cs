// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Threading;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using ChatApp.Models;
using ChatApp.WebApi.Interfaces;
using ChatApp.WebApi.Model;
using ChatApp.WebApi.Skills;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Microsoft.VisualStudio.Threading;

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0040

namespace ChatApp.WebApi.Services;

public class SemanticKernelApp : ISemanticKernelApp
{
    private readonly IConfiguration _configuration;
    private readonly ISecretStore _secretStore;
    private readonly IStateStore<ISemanticKernelSession> _stateStore;
   
    private readonly AsyncLazy<Kernel> _kernel;
    private readonly AzureOpenAIClient _openAIClient;
    private async Task<Kernel> InitKernelAsync()
    {
        var config = await SemanticKernelConfig.CreateAsync(_secretStore, CancellationToken.None);
        var builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Trace));
        
        if (config.AzureOpenAIConfig is AzureOpenAIConfig azureOpenAIConfig)
        {
            if (azureOpenAIConfig.Deployment is null || azureOpenAIConfig.Endpoint is null)
            {
                throw new InvalidOperationException("AzureOpenAI is enabled but AzureDeployment and AzureEndpoint are not set.");
            }
            builder.AddAzureOpenAIChatCompletion(azureOpenAIConfig.Deployment, _openAIClient);
        }

        builder
            .AddAzureOpenAITextEmbeddingGeneration(
                config.RagConfig.AzureOpenAIEmbeddingsDeploymentName,
                config.RagConfig.AzureOpenAIEmbeddingsEndpoint,
                apiKey: config.RagConfig.AzureOpenAIEmbeddingsApiKey
            )
            .AddAzureAISearchVectorStoreRecordCollection<KnowledgeBaseSnippet>(
                config.RagConfig.CollectionName,
                new Uri(config.RagConfig.AzureAISearchEndpoint),
                new AzureKeyCredential(config.RagConfig.AzureSearchApiKey))
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

        await AddSkillsAsync(builder);
        var kernel = builder.Build();
        //var vectorStoreTextSearch = kernel.Services.GetRequiredService<VectorStoreTextSearch<KnowledgeBaseSnippet>>();
        //builder.Plugins.Add(vectorStoreTextSearch.CreateWithGetTextSearchResults("KnowlegdebaseSearchPlugin", description: "allows finding TSG, playbooks and solutions to problems encountered before with kubernetes and Azure resources"));

        kernel.FunctionInvocationFilters.Add(new FunctionInvocationFilter(async (context, next) =>
        {
            Console.WriteLine($"Function {context.Function.Name} is about to be invoked.");
            await next(context);
            Console.WriteLine($"Function {context.Function.Name} was invoked.");
        }));
        return kernel;
    }

    private async Task<IKernelBuilder> AddSkillsAsync(IKernelBuilder builder)
    {
        TokenCredential tokenCredential =
            new ChainedTokenCredential(
                new AzureCliCredential(),
                new VisualStudioCredential(),
                new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions()));
        
        var tokenRequestContext = new TokenRequestContext(["499b84ac-1321-427f-aa17-267ca6975798/.default"], //read more here: https://learn.microsoft.com/en-us/rest/api/azure/devops/tokens/?view=azure-devops-rest-7.1&tabs=powershell
                                                            parentRequestId: null);
        var accesstoken = await tokenCredential.GetTokenAsync(tokenRequestContext, CancellationToken.None);

        builder.Plugins.AddFromType<KuberenetesSkills>();
        KernelPlugin adoPlugin = await OpenApiKernelPluginFactory.CreateFromOpenApiAsync(
            pluginName: "adopipelines",
            filePath: "ado_pipelines_openapi.json",
            new OpenApiFunctionExecutionParameters()
            {
                ServerUrlOverride = new Uri("https://microsoft.visualstudio.com/"),
                AuthCallback = (request, token) =>
                {
                    var requestUri = request.RequestUri;
                    request.RequestUri = new Uri(requestUri.ToString()
                        .Replace("https://microsoft.visualstudio.com/Microsoft/", "https://microsoft.visualstudio.com/"));

                    request.Headers.Accept.Add( new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accesstoken.Token);

                    return Task.CompletedTask;
                }
            }
        );
        builder.Plugins.Add(adoPlugin);
        builder.Plugins.AddFromType<KnowledgeBaseSkills>("KnowlegdebaseSearchPlugin");
        
        
        return builder;
    }

    public SemanticKernelApp(IConfiguration configuration, ISecretStore secretStore, IStateStore<ISemanticKernelSession> stateStore, AzureOpenAIClient openAIClient)
    {
        _configuration = configuration;
        _secretStore = secretStore;
        _stateStore = stateStore;
        _openAIClient = openAIClient;
        _kernel = new(() => Task.Run(InitKernelAsync));
    }

    public async Task<ISemanticKernelSession> CreateSession(Guid sessionId)
    {
        var kernel = await _kernel.GetValueAsync();
        var session = new SemanticKernelSession(_configuration, kernel, sessionId);
        await _stateStore.SetStateAsync(sessionId, session);
        return session;
    }

    public async Task<ISemanticKernelSession> GetSession(Guid sessionId)
    {
        var kernel = await _kernel.GetValueAsync();
        var state = await _stateStore.GetStateAsync(sessionId);
        if (state is null)
        {
            throw new KeyNotFoundException($"Session {sessionId} not found.");
        }
        return state;
    }
}