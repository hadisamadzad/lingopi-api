using Lingopi.Lingo.Application.Operations.Captures;
using Lingopi.Lingo.Application.Operations.LingoEnrichment;
using Lingopi.Lingo.Application.Operations.Lingos;
using Lingopi.Lingo.Application.Operations.UserSettings;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Interfaces;

public interface IOperationService
{
    IOperation<ProcessCaptureCommand, string> ProcessCapture { get; }
    IOperation<EnrichLingoCommand, string> EnrichLingo { get; }
    CaptureLingoOperation CaptureLingo { get; }
    GetLingoByIdOperation GetLingoById { get; }
    GetLingosByUserIdOperation GetLingosByUserId { get; }
    GetUserSettingsOperation GetUserSettings { get; }
    SaveUserSettingsOperation SaveUserSettings { get; }
}
