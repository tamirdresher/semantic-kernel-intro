// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using ChatApp.Models;
using ChatApp.WebApi.Interfaces;
using ChatApp.WebApi.Model;
using ChatApp.WebApi.Services;
using Microsoft.SemanticKernel.Data;
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureOpenAIClient("openAi");

builder.Services.AddSingleton<IStateStore<ISemanticKernelSession>>(new InMemoryStore<ISemanticKernelSession>());
builder.Services.AddSingleton<ISecretStore, ConfigurationBasedSecretStore>();
builder.Services.AddSingleton<ISemanticKernelApp, SemanticKernelApp>();



builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<AIChatRole>(JsonNamingPolicy.CamelCase)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
    
}
app.UseSwagger();
app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();
