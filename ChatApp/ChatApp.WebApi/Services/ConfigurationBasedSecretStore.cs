// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ChatApp.WebApi.Interfaces;

namespace ChatApp.WebApi.Services;

public class ConfigurationBasedSecretStore : ISecretStore
{
    private readonly IConfiguration _configuration;

    public ConfigurationBasedSecretStore(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken)
    {
#if !DEBUG
        throw new ApplicationException("EnvVarSecretStore should not be used in production.");
#else
        return Task.FromResult(_configuration.GetValue<string>(secretName) ?? "");
#endif
    }
}
