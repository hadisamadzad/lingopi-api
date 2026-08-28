using Lingopi.Lingo.Application.Models.Enums;

namespace Lingopi.Lingo.Api.Models;

public record CaptureLingoRequest(
    string Expression,
    string SourceLanguageCode,
    string SourceLocaleCode,
    LingoContext? Context = null
);
