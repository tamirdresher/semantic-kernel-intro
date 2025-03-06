var builder = DistributedApplication.CreateBuilder(args);

var useAzureOpenAI = bool.Parse(Environment.GetEnvironmentVariable("UseAzureOpenAI") ?? "true");
var indexKnowledgeBase = bool.Parse(Environment.GetEnvironmentVariable("IndexKnowledgeBase") ?? "false");

var azureDeployment = Environment.GetEnvironmentVariable("AzureDeployment") ?? "gpt-4o";
var azureEndpoint = Environment.GetEnvironmentVariable("AzureEndpoint") ?? "https://eastus.api.cognitive.microsoft.com/";

var azureSearchCollectionName = builder.AddParameter("AzureSearchCollectionName", secret: true);
var azureSearchEndpoint = builder.AddParameter("AzureSearchEndpoint", secret: true);
var azureSearchApiKey = builder.AddParameter("AzureSearchApiKey", secret: true);
var azureOpenAIEmbeddingsDeploymentName = builder.AddParameter("AzureOpenAIEmbeddingsDeploymentName", secret: true);
var azureOpenAIEmbeddingsEndpoint = builder.AddParameter("AzureOpenAIEmbeddingsEndpoint", secret: true);
var azureOpenAIEmbeddingsApiKey = builder.AddParameter("AzureOpenAIEmbeddingsApiKey", secret: true);
var adoOrganizationName = builder.AddParameter("ADOOrganizationName");
var adoProjectName = builder.AddParameter("ADOProjectName");

var openAi = builder.AddConnectionString("openAi");//.AddAzureOpenAI("openAi");
//.AddDeployment(new AzureOpenAIDeployment(azureDeployment, "gpt-4o", "2024-05-13"));

var backend = builder.AddProject<Projects.ChatApp_WebApi>("backend")
    .WithReference(openAi)
    .WithEnvironment("AzureDeployment", azureDeployment)
    .WithEnvironment("AzureEndpoint", azureEndpoint)
    .WithEnvironment("AzureSearchCollectionName", azureSearchCollectionName)
    .WithEnvironment("AzureSearchApiKey", azureSearchApiKey)
    .WithEnvironment("AzureSearchEndpoint", azureSearchEndpoint)
    .WithEnvironment("AzureOpenAIEmbeddingsDeploymentName", azureOpenAIEmbeddingsDeploymentName)
    .WithEnvironment("AzureOpenAIEmbeddingsEndpoint", azureOpenAIEmbeddingsEndpoint)
    .WithEnvironment("AzureOpenAIEmbeddingsApiKey", azureOpenAIEmbeddingsApiKey)
    .WithEnvironment("ADOOrganizationName", adoOrganizationName)
    .WithEnvironment("ADOProjectName", adoProjectName);

var frontend = builder.AddNpmApp("frontend", "../ChatApp.React")
    .WithReference(backend)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

if (indexKnowledgeBase)
{
    builder.AddProject<Projects.KnowledgeBaseIndexer>("knowledgebaseindexer");
}

builder.Build().Run();
