using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.ValueObjects;

namespace Lingopi.Lingo.Application.Models.ReadModels;

public static class LingoModelMapper
{
    public static LingoModel MapToLingoModel(this LingoEntity entity)
    {
        return new LingoModel(
            Id: entity.Id,
            UserId: entity.UserId,
            Lingo: entity.Lingo,
            LingoType: entity.LingoType,
            Definition: entity.Definition,
            Translation: entity.Translation,
            SourceLanguage: new LanguageValue(
                entity.Languages.Source.Code,
                entity.Languages.Source.Name,
                entity.Languages.Source.NativeName),
            TargetLanguage: new LanguageValue(
                entity.Languages.Target.Code,
                entity.Languages.Target.Name,
                entity.Languages.Target.NativeName),
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

    public static IEnumerable<LingoModel> MapToLingoModels(this List<LingoEntity> entities)
    {
        foreach (var entity in entities)
        {
            yield return entity.MapToLingoModel();
        }
    }
}
