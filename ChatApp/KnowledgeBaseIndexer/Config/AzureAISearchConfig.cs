using System.ComponentModel.DataAnnotations;

internal sealed class AzureAISearchConfig
{
    public const string ConfigSectionName = "AzureAISearch";

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}