using Lingopi.Lingo.Application.Models.Configs;

namespace Lingopi.Lingo.Application.Interfaces;

public interface IOpenAIModelSettingsProvider
{
    OpenAIModelSettings Get(string? modelRole);

    IReadOnlyCollection<OpenAIModelSettings> GetAll();

    void Replace(IEnumerable<OpenAIModelSettings> settings, string defaultModelRole);
}
