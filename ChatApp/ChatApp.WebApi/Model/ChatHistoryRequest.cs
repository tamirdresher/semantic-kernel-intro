using System.Text.Json.Serialization;

namespace ChatApp.WebApi.Model;

public record ChatHistoryRequest()
{
    [JsonInclude, JsonPropertyName("sessionId")]
    public Guid? SessionId;

}