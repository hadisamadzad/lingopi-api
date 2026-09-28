namespace Lingopi.Lingo.Application.Models.Configs;

public static class OpenAIModelRoles
{
    public const string Economy = "Economy";
    public const string Premium = "Premium";
    public const string Embedding = "Embedding";
}

public sealed record OpenAIModelSettings(
    string Role,
    string ModelId,
    decimal? InputCostPerMillionTokens = null,
    decimal? OutputCostPerMillionTokens = null);
