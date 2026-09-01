using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.ReadModels;
using Lingopi.Lingo.Application.Operations.Captures;
using Lingopi.Lingo.Application.Operations.LingoEnrichment;
using Lingopi.Lingo.Application.Operations.Lingos;
using Lingopi.Lingo.Application.Operations.UserSettings;
using Lingopi.Lingo.Application.Operations.UserUsage;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Operations;

public class OperationService(
    IOperation<ProcessCaptureCommand, string> processCapture,
    IOperation<EnrichLingoCommand, string> enrichLingo,
    IOperation<CaptureLingoCommand, string> captureLingo,
    IOperation<GetLingoByIdCommand, LingoModel> getLingoById,
    IOperation<GetLingosByUserIdCommand, List<LingoModel>> getLingosByUserId,
    IOperation<GetUserSettingsCommand, UserSettingsModel> getUserSettings,
    IOperation<SaveUserSettingsCommand, UserSettingsModel> saveUserSettings,
    IOperation<GetUserUsageSummaryCommand, UserUsageSummaryModel> getUserUsageSummary
) : IOperationService
{
    public IOperation<ProcessCaptureCommand, string> ProcessCapture { get; } = processCapture;
    public IOperation<EnrichLingoCommand, string> EnrichLingo { get; } = enrichLingo;
    public CaptureLingoOperation CaptureLingo { get; } =
        captureLingo as CaptureLingoOperation ?? throw new ArgumentNullException(nameof(captureLingo));
    public GetLingoByIdOperation GetLingoById { get; } =
        getLingoById as GetLingoByIdOperation ?? throw new ArgumentNullException(nameof(getLingoById));
    public GetLingosByUserIdOperation GetLingosByUserId { get; } =
        getLingosByUserId as GetLingosByUserIdOperation ?? throw new ArgumentNullException(nameof(getLingosByUserId));
    public GetUserSettingsOperation GetUserSettings { get; } =
        getUserSettings as GetUserSettingsOperation ?? throw new ArgumentNullException(nameof(getUserSettings));
    public SaveUserSettingsOperation SaveUserSettings { get; } =
        saveUserSettings as SaveUserSettingsOperation ?? throw new ArgumentNullException(nameof(saveUserSettings));
    public GetUserUsageSummaryOperation GetUserUsageSummary { get; } =
        getUserUsageSummary as GetUserUsageSummaryOperation ??
        throw new ArgumentNullException(nameof(getUserUsageSummary));
}
