using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.ValueObjects;

namespace Lingopi.Lingo.Application.Models.ReadModels;

public static class LingoModelMapper
{
     public static LingoModel MapToLingoModel(this LingoEntity entity, Dictionary<string, LanguageEntity> languageEntities)
    {
        var sourceLanguage = languageEntities.GetValueOrDefault(entity.Languages.SourceLanguageId);
        var targetLanguage = languageEntities.GetValueOrDefault(entity.Languages.TargetLanguageId);

        return new LingoModel(
            Id: entity.Id,
            UserId: entity.UserId,
            Lingo: entity.Lingo,
            LingoType: entity.LingoType,
            Definition: entity.Definition,
            Translation: entity.Translation,
            SourceLanguage: new LanguageValue(
                entity.Languages.SourceLanguageId,
                sourceLanguage?.Name ?? string.Empty,
                sourceLanguage?.NativeName ?? string.Empty),
            TargetLanguage: new LanguageValue(
                entity.Languages.TargetLanguageId,
                targetLanguage?.Name ?? string.Empty,
                targetLanguage?.NativeName ?? string.Empty),
            Style: entity.Style,
            Examples: entity.Examples,
            Context: entity.Context,
            Tags: entity.Tags,
            LearningGoal: entity.LearningGoal,
            UserNote: entity.UserNote,
            SourceMethod: entity.Source?.Method,
            SourceModel: entity.Source?.Model,
            SourceVersion: entity.Source?.Version,
            ReviewLastTime: entity.Review.LastTime,
            ReviewNextTime: entity.Review.NextTime,
            ReviewRepetitions: entity.Review.Repetitions,
            ReviewSrsLevel: entity.Review.SrsLevel,
            CreatedAt: entity.Audit.CreatedAt,
            UpdatedAt: entity.Audit.UpdatedAt
        );
    }
}
