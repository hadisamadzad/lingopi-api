using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Configs;
using Microsoft.Extensions.Options;

namespace Lingopi.Lingo.Infrastructure.OpenAI;

public sealed class OpenAIModelSettingsProvider : IOpenAIModelSettingsProvider
{
    private readonly object _sync = new();
    private Dictionary<string, OpenAIModelSettings> _settings;
    private string _defaultModel;

    public OpenAIModelSettingsProvider(IOptions<OpenAIConfig>? options = null)
    {
        var config = options?.Value;
        var settings = config?.Models is { Count: > 0 }
            ? config.Models
            :
            [
                new OpenAIModelSettings(OpenAIModels.Gpt56Luna),
                new OpenAIModelSettings(OpenAIModels.Gpt56Terra),
                new OpenAIModelSettings(OpenAIModels.Gpt5Nano),
                new OpenAIModelSettings(
                    OpenAIModels.TextEmbedding3Small,
                    InputCostPerMillionTokens: 0.02m)
            ];

        _settings = CreateDictionary(settings);
        _defaultModel = config?.DefaultModel ?? OpenAIModels.Gpt56Luna;
    }

    public OpenAIModelSettings Get(string? model)
    {
        lock (_sync)
        {
            var modelToUse = string.IsNullOrWhiteSpace(model) ? _defaultModel : model.Trim();

            return _settings.TryGetValue(modelToUse, out var settings)
                ? settings
                : throw new ArgumentException($"OpenAI model '{modelToUse}' is not configured.", nameof(model));
        }
    }

    public IReadOnlyCollection<OpenAIModelSettings> GetAll()
    {
        lock (_sync)
        {
            return [.. _settings.Values];
        }
    }

    public void Replace(
        IEnumerable<OpenAIModelSettings> settings,
        string defaultModel)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultModel);

        var replacement = CreateDictionary(settings);
        if (!replacement.ContainsKey(defaultModel.Trim()))
        {
            throw new ArgumentException(
                $"The default OpenAI model '{defaultModel}' is not configured.",
                nameof(defaultModel));
        }

        lock (_sync)
        {
            _settings = replacement;
            _defaultModel = defaultModel.Trim();
        }
    }

    private static Dictionary<string, OpenAIModelSettings> CreateDictionary(
        IEnumerable<OpenAIModelSettings> settings)
    {
        var result = new Dictionary<string, OpenAIModelSettings>(StringComparer.OrdinalIgnoreCase);

        foreach (var setting in settings)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(setting.Model);
            if (!result.TryAdd(setting.Model.Trim(), setting with { Model = setting.Model.Trim() }))
            {
                throw new ArgumentException(
                    $"OpenAI model '{setting.Model}' is configured more than once.",
                    nameof(settings));
            }
        }

        return result.Count > 0
            ? result
            : throw new ArgumentException("At least one OpenAI model must be configured.", nameof(settings));
    }
}
