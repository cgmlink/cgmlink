using System;
using System.Text.Json;

namespace CgmLink.Nutrition.FatSecretClient.Exceptions;

public class FatSecretApiException : Exception
{
    public FatSecretApiException(
        string message,
        int statusCode = 0,
        string? errorCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public int StatusCode { get; }

    public string? ErrorCode { get; }

    public static FatSecretApiException FromResponse(string content, int statusCode)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new FatSecretApiException($"FatSecret API returned status code {statusCode}.", statusCode);
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            return FromResponse(document.RootElement, statusCode);
        }
        catch (JsonException exception)
        {
            return new FatSecretApiException(
                $"FatSecret API returned status code {statusCode}.",
                statusCode,
                innerException: exception);
        }
    }

    private static FatSecretApiException FromResponse(JsonElement root, int statusCode)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("error", out var errorData))
        {
            return new FatSecretApiException("FatSecret API returned an error.", statusCode);
        }

        string? errorCode;
        string? message;
        if (errorData.ValueKind == JsonValueKind.Object)
        {
            errorCode = errorData.TryGetProperty("code", out var codeElement)
                ? GetString(codeElement)
                : null;
            message = errorData.TryGetProperty("message", out var messageElement)
                ? GetString(messageElement)
                : null;
        }
        else
        {
            errorCode = GetString(errorData);
            message = errorCode;
        }

        if (root.TryGetProperty("error_description", out var descriptionElement))
        {
            var description = GetString(descriptionElement);
            if (!string.IsNullOrWhiteSpace(description))
            {
                message = description;
            }
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            message = errorCode;
        }

        return new FatSecretApiException(
            string.IsNullOrWhiteSpace(message) ? "FatSecret API returned an error." : message,
            statusCode,
            errorCode);
    }

    private static string? GetString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null,
        };
    }
}
