namespace Lingopi.Lingo.Application.Models.Configs;

public static class OpenAIModels
{
    public const string Gpt6Sol = "gpt-6-sol";
    public const string Gpt6Luna = "gpt-6-luna";
    public const string TextEmbedding3Small = "text-embedding-3-small";
}

public sealed record OpenAIModelSettings(
    string ModelId,
    decimal? InputCostPerMillionTokens = null,
    decimal? OutputCostPerMillionTokens = null);
