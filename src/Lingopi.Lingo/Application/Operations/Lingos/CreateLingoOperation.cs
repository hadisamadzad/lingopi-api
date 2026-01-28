using Damas.Operations;
using Lingopi.Core.Helpers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ValueObjects;

namespace Lingopi.Lingo.Application.Operations.Lingos;

public class CreateLingoOperation(IRepositoryManager repository) :
    IOperation<CreateLingoCommand, string>
{
    public async Task<OperationResult<string>> ExecuteAsync(
        CreateLingoCommand command, CancellationToken? cancellation = null)
    {
        // Create entity
        var entity = new LingoEntity
        {
            Id = UidHelper.GenerateNewId("lingo"),
            UserId = command.UserId,
            Lingo = command.Lingo,
            LingoType = command.LingoType,
            Definition = command.Definition,
            Translation = command.Translation,
            Style = command.Style,
            Examples = command.Examples ?? [],
            Context = command.Context ?? [],
            Tags = command.Tags ?? [],
            LearningGoal = command.LearningGoal,
            UserNote = command.UserNote,
            Languages = new LanguagesValue
            {
                SourceLanguageId = command.SourceLanguageId,
                TargetLanguageId = command.TargetLanguageId,

            },
            Review = new ReviewValue
            {
                LastTime = null,
                NextTime = null,
                Repetitions = 0,
                SrsLevel = 1
            },
            Source = command.SourceMethod != null ? new SourceValue
            {
                Method = command.SourceMethod.Value,
                Model = command.SourceModel,
                Version = command.SourceVersion
            } : null,
            Audit = new AuditValue
            {
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            }
        };

        // Save to database using base repository method
        await repository.Lingos.InsertAsync(entity);

        return OperationResult<string>.Success(entity.Id);
    }
}

public record CreateLingoCommand(
    string UserId,
    string Lingo,
    LingoType LingoType,
    string Definition,
    string Translation,
    string SourceLanguageId,
    string TargetLanguageId
) : IOperationCommand
{
    public WordStyle? Style { get; init; }
    public List<string>? Examples { get; init; }
    public List<Context>? Context { get; init; }
    public List<string>? Tags { get; init; }
    public LearningGoal? LearningGoal { get; init; }
    public string? UserNote { get; init; }
    public SourceMethod? SourceMethod { get; init; }
    public string? SourceModel { get; init; }
    public string? SourceVersion { get; init; }
}
