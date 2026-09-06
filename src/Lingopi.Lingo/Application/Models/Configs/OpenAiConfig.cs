namespace Lingopi.Lingo.Application.Models.Configs;

public sealed class OpenAIConfig
{
    public const string Key = "OpenAI";

    public string BaseAddress { get; set; } = "https://api.openai.com/v1/";
    public string ApiKey { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = "translation-v1";
    public int TimeoutSeconds { get; set; } = 60;
    public string DefaultModel { get; set; } = OpenAIModels.Gpt56Luna;
    public List<OpenAIModelSettings> Models { get; set; } = [];
}
