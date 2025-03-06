// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

using ChatApp.WebApi.Interfaces;
using ChatApp.WebApi.Model;

namespace ChatApp.WebApi.Controllers;

[ApiController, Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ISemanticKernelApp _semanticKernelApp;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ISemanticKernelApp semanticKernelApp, ILogger<ChatController> logger)
    {
        _semanticKernelApp = semanticKernelApp;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> ProcessMessage(AIChatRequest request)
    {
        var session = request.SessionState switch
        {
            Guid sessionId => await _semanticKernelApp.GetSession(sessionId),
            _ => await _semanticKernelApp.CreateSession(Guid.NewGuid())
        };
        try
        {
            var response = await session.ProcessRequest(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"couldnt process message");
            throw;
        }
    }

    [HttpPost("getLatestMessages")]
    [Consumes("application/json")]
    public async Task<IActionResult> GetLatestMessages([FromBody] ChatHistoryRequest request)
    {
        if (request.SessionId is null)
        {
            return BadRequest("no active session found");
        }

        var session = await _semanticKernelApp.GetSession(request.SessionId.Value);
        
        var response = session.GetFullChatMessagesSinceLastRetrieval();
        return Ok(response);
    }


}
