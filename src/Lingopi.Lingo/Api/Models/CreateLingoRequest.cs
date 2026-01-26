using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ValueObjects;

namespace Lingopi.Lingo.Api.Models;

public record CreateLingoRequest(
    string UserId,
    string Lingo,
    LingoType LingoType,
    string Definition,
    string Translation,
    LanguageValue SourceLanguage,
    LanguageValue TargetLanguage
)
{
    public WordStyle? Style { get; init; }
    public List<string>? Examples { get; init; }
    public List<Context>? Context { get; init; }
    public List<string>? Tags { get; init; }
    public LearningGoal? LearningGoal { get; init; }
    public string? UserNote { get; init; }
    public SourceMethod? SourceMethod { get; init; }
    public string? SourceAIModel { get; init; }
    public string? SourceAIModelVersion { get; init; }
}
