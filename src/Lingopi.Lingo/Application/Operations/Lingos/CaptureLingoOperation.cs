using Lingopi.Core.Extensions;
using Lingopi.Core.Helpers;
using Lingopi.Lingo.Application.Helpers;
using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Services;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Operations.Lingos.Validators;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Operations.Lingos;

public class CaptureLingoOperation(
    IRepositoryManager repository,
    IEntitlementService? entitlementService = null,
    TimeProvider? timeProvider = null) :
    IOperation<CaptureLingoCommand, string>
{
    public async Task<OperationResult<string>> ExecuteAsync(
        CaptureLingoCommand command, CancellationToken? cancellation = null)
    {
        // Validate the command
        var validation = new CaptureLingoCommandValidator().Validate(command);
        if (!validation.IsValid)
        {
            return OperationResult<string>.ValidationFailure([.. validation.GetErrorMessages()]);
        }

        if (entitlementService is not null)
        {
            var authorization = await entitlementService.AuthorizeCaptureAsync(
                command.UserId,
                (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime,
                cancellation ?? CancellationToken.None);
            if (!authorization.IsAllowed)
            {
                return OperationResult<string>.AuthorizationFailure(
                    $"{authorization.ErrorCode ?? "capture_not_authorized"}: " +
                    (authorization.ErrorMessage ?? "Capture is not available for this user."));
            }
        }

        // Load user settings to determine the default locales
        var userSettings = await repository.UserSettings.GetByUserIdAsync(command.UserId);
        if (userSettings is null)
        {
            return OperationResult<string>.ValidationFailure(
                $"Lingo settings for user '{command.UserId}' must be configured before capture.");
        }

        var sourceLocaleCode = LocaleCodeNormalizer.Normalize(command.SourceLocaleCode);
        var targetLocaleCode = userSettings.TargetLocaleCode;

        if (sourceLocaleCode is null ||
            !userSettings.SourceLocaleCodes.Any(x =>
                string.Equals(x, sourceLocaleCode, StringComparison.OrdinalIgnoreCase)))
        {
            return OperationResult<string>.ValidationFailure(
                $"Source locale '{sourceLocaleCode}' is not configured for user's target locale '{targetLocaleCode}'.");
        }

        // Insert into the database
        var captureId = UidHelper.GenerateNewId("capture");
        var capture = new CaptureEntity
        {
            Id = captureId,
            UserId = command.UserId,
            Expression = command.Expression,
            EncounterContext = command.EncounterContext,
            SourceLanguageCode = command.SourceLanguageCode,
            SourceLocaleCode = sourceLocaleCode,
            TargetLocaleCode = targetLocaleCode,
            Status = CaptureAnalysisStatus.AnalysisQueued,
            Audit = new CaptureAuditValue
            {
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await repository.Captures.InsertAsync(capture);

        return OperationResult<string>.Success(captureId);
    }
}

public record CaptureLingoCommand(
    string UserId,
    string Expression,
    string SourceLocaleCode,
    string SourceLanguageCode,
    LingoContext? EncounterContext = null
) : IOperationCommand;
