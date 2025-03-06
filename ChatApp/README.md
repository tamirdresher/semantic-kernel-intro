This app is based on the code from https://github.com/Azure-Samples/aspire-semantic-kernel-basic-chat-app

it shows a  comprehensive example of a chat application built with Semantic Kernel that allows interaction with kubectl (via command line), ADO pipelines and an external knowledge base that is stored in Azure AI Search. 
The frontend of the application is developed using React and Vit and interact with the chat backend using @microsoft/ai-chat-protocol package. 

To run the app:
1. run "npm install" inside the ChatApp.React folder
2. Deploy embedding model (such as text-embedding-ada-002) and Chat completion model (such as gpt-o1) in Azure AI Foundry https://ai.azure.com/
4. Create an Azure AI Search resource https://learn.microsoft.com/en-us/azure/search/search-create-service-portal
3. Create a User Secrets file for the ChatApp.AppHost project 
```
{
  "Parameters": {    
    "AzureSearchCollectionName": "[The name of the collection the knowledge base is stored in]",
    "AzureSearchEndpoint": "[The endpoint of the Azure AI Search]",
    "AzureSearchApiKey": "[The endpoint of the Azure AI Search]",

    "AzureOpenAIEmbeddingsEndpoint": "https://eastus.api.cognitive.microsoft.com/",
    "AzureOpenAIEmbeddingsDeploymentName": "[Embedding model deployment name]",
    "AzureOpenAIEmbeddingsApiKey": "[Embedding model endpoint]"
	
  },
  "ConnectionStrings": {
    "openAi": "Endpoint=[Chat Completion model endpoint];Key=[The Chat Completion model api key];"
  }

}
```
You can also set the secrets using the command line
```
dotnet user-secrets set YourSecretName "YourSecretContent"
```
for example
```
dotnet user-secrets set "Parameters:AzureSearchCollectionName" "mu_knowledgebase"
```
5. Run the the Aspire dashboard by selecting ChatApp.AppHost as startup project 