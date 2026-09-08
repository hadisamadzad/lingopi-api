using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Api.Models;

public record LanguageResponse(
    string Id,
    string Code,
    string Name,
    string NativeName,
    bool IsActive,
    List<LocaleResponse> Locales);

public record LocaleResponse(
    string Code,
    string Region,
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive);

public record UpsertLanguageRequest(
    string Code,
    string Name,
    string NativeName,
    bool IsActive,
    List<LocaleRequestModel> Locales);

public record LocaleRequestModel(
    string Code,
    string Region,
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive);

public record SetLanguageActivationRequest(bool IsActive);

public static class LanguageResponseMapper
{
    public static LanguageResponse ToResponse(this LanguageReadModel model)
    {
        return new LanguageResponse(
            model.Id,
            model.Code,
            model.Name,
            model.NativeName,
            model.IsActive,
            model.Locales.Select(locale => locale.ToResponse()).ToList());
    }

    private static LocaleResponse ToResponse(this LocaleReadModel model)
    {
        return new LocaleResponse(
            model.Code,
            model.Region,
            model.Name,
            model.NativeName,
            model.IsRightToLeft,
            model.IsActive);
    }
}
