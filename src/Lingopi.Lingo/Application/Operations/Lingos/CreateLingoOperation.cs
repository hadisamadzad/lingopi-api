using Lingopi.Core.Helpers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Operations.Lingos.Validators;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Operations.Lingos;

public class CreateLingoOperation(IRepositoryManager repository) :
    IOperation<CreateLingoCommand, string>
{
    public async Task<OperationResult<string>> ExecuteAsync(
        CreateLingoCommand command, CancellationToken? cancellation = null)
    {
        var validation = new CreateLingoCommandValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult<string>.ValidationFailure([.. validation.GetErrorMessages()]);
        }

        var now = DateTime.UtcNow;
        var lingoId = UidHelper.GenerateNewId("lingo");
        var processingJobId = UidHelper.GenerateNewId("lingo-job");

        var entity = new LingoEntity
        {
            Id = lingoId,
            UserId = command.UserId,
            Capture = new CaptureValue
            {
                OriginalText = command.OriginalText,
                SourceLocaleCode = command.SourceLocaleCode,
                CapturedAt = now
            },
            Content = null,
            Learning = new LearningValue
            {
                Goal = null,
                Status = LearningStatus.NotStarted,
                CurrentReviewState = LearningReviewState.New,
                Review = new SrsReviewValue
                {
                    LastReviewedAt = null,
                    NextReviewAt = null,
                    Repetitions = 0,
                    Level = 1
                }
            },
            Processing = new ProcessingValue
            {
                Status = ProcessingStatus.Queued,
                CurrentJobId = processingJobId,
                LastProcessedAt = null,
                ErrorCode = null,
                ErrorMessage = null
            },
            Suggestions = [],
            Audit = new AuditValue
            {
                CreatedAt = now,
                UpdatedAt = now,
                Version = 1,
                SchemaVersion = AuditValue.CurrentSchemaVersion
            }
        };

        var processingJob = new LingoProcessingJobEntity
        {
            Id = processingJobId,
            LingoId = lingoId,
            UserId = command.UserId,
            Type = ProcessingJobType.EnrichLingo,
            Status = ProcessingJobStatus.Queued,
            InputRevision = entity.Audit.Version,
            AttemptCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.Lingos.InsertAsync(entity);
        await repository.ProcessingJobs.InsertAsync(processingJob);

        return OperationResult<string>.Success(lingoId);
    }
}

public record CreateLingoCommand(
    string UserId,
    string OriginalText,
    string? SourceLocaleCode
) : IOperationCommand;
