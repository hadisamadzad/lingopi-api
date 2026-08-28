using Lingopi.Lingo.Application.Models.Configs;

namespace Lingopi.Lingo.Application.Interfaces;

public interface IOpenAIModelSettingsProvider
{
    OpenAIModelSettings Get(string? model);

    IReadOnlyCollection<OpenAIModelSettings> GetAll();

    void Replace(IEnumerable<OpenAIModelSettings> settings, string defaultModel);
}
