using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Configs;
using Microsoft.Extensions.Options;

namespace Lingopi.Lingo.Infrastructure.OpenAI;

public sealed class OpenAIModelSettingsProvider : IOpenAIModelSettingsProvider
{
    private readonly object _sync = new();
    private Dictionary<string, OpenAIModelSettings> _settings;
    private string _defaultModelRole;

    public OpenAIModelSettingsProvider(IOptions<OpenAIConfig> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var config = options.Value;
        _settings = CreateDictionary(config.Models);
        _defaultModelRole = GetConfiguredDefaultRole(_settings, config.DefaultModelRole);
    }

    public OpenAIModelSettings Get(string? modelRole)
    {
        lock (_sync)
        {
            var roleToUse = string.IsNullOrWhiteSpace(modelRole) ? _defaultModelRole : modelRole.Trim();

            return _settings.TryGetValue(roleToUse, out var settings)
                ? settings
                : throw new ArgumentException(
                    $"OpenAI model role '{roleToUse}' is not configured.",
                    nameof(modelRole));
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
        string defaultModelRole)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultModelRole);

        var replacement = CreateDictionary(settings);
        var configuredDefaultRole = GetConfiguredDefaultRole(replacement, defaultModelRole);

        lock (_sync)
        {
            _settings = replacement;
            _defaultModelRole = configuredDefaultRole;
        }
    }

    private static string GetConfiguredDefaultRole(
        IReadOnlyDictionary<string, OpenAIModelSettings> settings,
        string defaultModelRole)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultModelRole);

        var normalizedRole = defaultModelRole.Trim();
        if (!settings.ContainsKey(normalizedRole))
        {
            throw new ArgumentException(
                $"Default OpenAI model role '{defaultModelRole}' is not configured.",
                nameof(defaultModelRole));
        }

        return normalizedRole;
    }

    private static Dictionary<string, OpenAIModelSettings> CreateDictionary(
        IEnumerable<OpenAIModelSettings> settings)
    {
        var result = new Dictionary<string, OpenAIModelSettings>(StringComparer.OrdinalIgnoreCase);

        foreach (var setting in settings)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(setting.Role);
            ArgumentException.ThrowIfNullOrWhiteSpace(setting.ModelId);
            if (!result.TryAdd(
                    setting.Role.Trim(),
                    setting with
                    {
                        Role = setting.Role.Trim(),
                        ModelId = setting.ModelId.Trim()
                    }))
            {
                throw new ArgumentException(
                    $"OpenAI model role '{setting.Role}' is configured more than once.",
                    nameof(settings));
            }
        }

        return result.Count > 0
            ? result
            : throw new ArgumentException("At least one OpenAI model must be configured.", nameof(settings));
    }
}
