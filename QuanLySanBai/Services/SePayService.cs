using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Services;

public class SePayService(IOptions<SePayOptions> options)
{
    private static readonly Regex CodeRegex = new(@"\b(QSB[A-Z0-9]{6,40})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FallbackCodeRegex = new(@"(QSB(?:W[A-Z0-9]{12}|O\d+T[A-Z0-9]{8}))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public SePayOptions Settings => options.Value;

    public bool VerifyAuthorization(string? authorizationHeader)
    {
        var expectedKey = Settings.WebhookApiKey?.Trim();
        if (string.IsNullOrEmpty(expectedKey) || string.IsNullOrWhiteSpace(authorizationHeader))
            return false;

        const string prefix = "Apikey ";
        if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var incomingKey = authorizationHeader[prefix.Length..].Trim();
        if (incomingKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            incomingKey = incomingKey[prefix.Length..].Trim();

        var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);
        var incomingBytes = Encoding.UTF8.GetBytes(incomingKey);

        return expectedBytes.Length == incomingBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, incomingBytes);
    }

    public SePayWebhookPayload? ParsePayload(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<SePayWebhookPayload>(rawJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public bool AccountMatches(string? incomingAccount, string? subAccount = null)
    {
        var configured = NormalizeAccount(Settings.AccountNumber);
        if (string.IsNullOrEmpty(configured))
            return false;

        var primary = NormalizeAccount(incomingAccount);
        var sub = NormalizeAccount(subAccount);

        return string.Equals(primary, configured, StringComparison.OrdinalIgnoreCase) ||
               (!string.IsNullOrEmpty(sub) && string.Equals(sub, configured, StringComparison.OrdinalIgnoreCase));
    }

    public string? ExtractPaymentCode(SePayWebhookPayload payload)
    {
        string?[] candidates = [payload.Code?.Trim(), payload.Content, payload.Description];
        foreach (var regex in (Regex[])[CodeRegex, FallbackCodeRegex])
        {
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                var match = regex.Match(candidate);
                if (match.Success)
                    return match.Groups[1].Value.ToUpperInvariant();
            }
        }

        return null;
    }

    private static string NormalizeAccount(string? value) =>
        Regex.Replace(value ?? "", @"[\s\-]", "");
}
