using System.Text.Json;
using System.Text.Json.Serialization;
using CgmLink.Nutrition.FatSecretClient.Exceptions;

namespace CgmLink.Nutrition.FatSecretClient.Json;

internal static class FatSecretJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    internal static T? Deserialize<T>(string content, int statusCode, string invalidResponseMessage)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out _))
            {
                throw FatSecretApiException.FromResponse(content, statusCode);
            }

            return document.RootElement.Deserialize<T>(Options);
        }
        catch (JsonException exception)
        {
            throw new FatSecretApiException(
                invalidResponseMessage,
                statusCode,
                innerException: exception);
        }
    }
}
