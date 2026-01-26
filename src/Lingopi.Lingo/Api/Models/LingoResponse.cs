using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ValueObjects;

namespace Lingopi.Lingo.Api.Models;

public record LingoResponse(
    string LingoId,
    string UserId,
    string Lingo,
    LingoType LingoType,
    string Definition,
    string Translation,
    LanguageValue SourceLanguage,
    LanguageValue TargetLanguage,
    WordStyle? Style,
    List<string> Examples,
    List<Context> Context,
    List<string> Tags,
    LearningGoal? LearningGoal,
    string? UserNote,
    SourceMethod? SourceMethod,
    string? SourceModel,
    string? SourceVersion,
    DateTime? ReviewLastTime,
    DateTime? ReviewNextTime,
    int ReviewRepetitions,
    int ReviewSrsLevel,
    DateTime CreatedAt,
    DateTime UpdatedAt
);