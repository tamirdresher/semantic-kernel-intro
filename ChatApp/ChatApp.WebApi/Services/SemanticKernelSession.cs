using System.Diagnostics;
using System.Text;
using ChatApp.WebApi.Interfaces;
using ChatApp.WebApi.Model;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Data;
#pragma warning disable SKEXP0001

namespace ChatApp.WebApi.Services;

internal class SemanticKernelSession : ISemanticKernelSession
{
    private readonly IConfiguration _configuration;
    private readonly Kernel _kernel;

    private readonly ChatHistory _chatHistory = new ChatHistory();
    private int _lastrRetriveIndex = 0;

    public Guid Id { get; private set; }

    internal SemanticKernelSession(IConfiguration configuration, Kernel kernel, Guid sessionId)
    {
        _configuration = configuration;
        _kernel = kernel;
        Id = sessionId;
        var adoOrgName = _configuration.GetValue<string>("ADOOrganizationName");
        var adoProjName = _configuration.GetValue<string>("ADOProjectName");
        var systemPrompt = $"""
            ChatBot can have a conversation with you about any topic.
            It can give explicit instructions or say 'I don't know' if it does not know the answer.
            ChatBot can run functions and plugin when needed and should search for solution and relevent information about problems using the KnowlegdebaseSearchPlugin.
            Prefer searching in the knowledgebase first, before providing your final answer.
            Wheneven you need to do somethine related to Azure DevOps(ADO) assume that the organization is {adoOrgName} and Project name is {adoProjName}
            """;
        _chatHistory.AddSystemMessage(systemPrompt);
    }

    //private string BuildSystemPrompt()
    //{
    //    var adoOrgName = _configuration.GetValue<string>("ADOOrganizationName");
    //    var adoProjName = _configuration.GetValue<string>("ADOProjectName");

    //    systemPrompt.All
    //}



    public async Task<AIChatCompletion> ProcessRequest(AIChatRequest message)
    {


        _chatHistory.AddUserMessage(message.Messages.Last().Content);
        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        var settings = new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false) };

        var messageContent = await chatCompletionService.GetChatMessageContentAsync(_chatHistory, settings, _kernel);
        var botResponse = messageContent;
        var functionCalls = FunctionCallContent.GetFunctionCalls(messageContent).ToArray();

        while (functionCalls.Length != 0)
        {
            // Adding function call from AI model to chat history
            _chatHistory.Add(messageContent);

            // Iterating over the requested function calls and invoking them
            foreach (var functionCall in functionCalls)
            {
                var result = await functionCall.InvokeAsync(_kernel);

                _chatHistory.Add(result.ToChatMessage());
            }

            // Sending the functions invocation results to the AI model to get the final response
            messageContent = await chatCompletionService.GetChatMessageContentAsync(_chatHistory, settings, _kernel);
            functionCalls = FunctionCallContent.GetFunctionCalls(messageContent).ToArray();
        }
        _chatHistory.Add(messageContent);

        return new AIChatCompletion(Message: new AIChatMessage
        {
            Role = AIChatRole.Assistant,
            Content = $"{messageContent}",
        })
        {
            SessionState = Id,
        };
    }

    public ChatMessage[] GetFullChatMessagesSinceLastRetrieval()
    {
        var result = _chatHistory.Skip(_lastrRetriveIndex)
            .SelectMany(msg =>
            {
                var content = string.Empty; // Variable assigned but not used
                List<ChatMessage> messages =
                [
                    new ChatMessage()
                    {
                        Role = msg.Role.ToString(),
                        Content = msg.ToString()
                    }

                ];

                messages.AddRange(
                    msg.Items.Select(item =>
                    {
                        switch (item)
                        {
                            case FunctionCallContent funcCall:
                                {
                                    var args = string.Concat(funcCall.Arguments?.Select(a => $"{a.Key}={a.Value},") ?? Enumerable.Empty<string>());
                                    return $"{funcCall.PluginName}.{funcCall.FunctionName}({args})";
                                }
                            case FunctionResultContent funcResult:
                                return funcResult.Result switch
                                {
                                    TextSearchResult[] searchResults => string.Concat(searchResults.Select(result => $"Link:{result.Link}\nContent:\n{result.Value}\n\n")),
                                    _=> funcResult.InnerContent?.ToString() ?? string.Empty
                                };
                                //return funcResult.InnerContent.ToString();
                            default:    
                                return "";
                        }
                    })
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(message => new ChatMessage()
                    {
                        Role = msg.Role.ToString(),
                        Content = message
                    }));


                return messages;

            }).ToArray();

        _lastrRetriveIndex = _chatHistory.Count - 1;
        return result;
    }



}