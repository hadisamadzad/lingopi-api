namespace Lingopi.Lingo.Application.Models.Configs;

public static class OpenAIModels
{
    public const string Gpt56Terra = "gpt-5.6-terra";
    public const string Gpt56Luna = "gpt-5.6-luna";
    public const string Gpt5Nano = "gpt-5-nano";
    public const string TextEmbedding3Small = "text-embedding-3-small";
}

public sealed record OpenAIModelSettings(
    string ModelId,
    decimal? InputCostPerMillionTokens = null,
    decimal? OutputCostPerMillionTokens = null);
