using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Api.Models;

public record LanguageResponse(
    string Id,
    string LocaleCode,
    string LanguageCode,
    string RegionCode,
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastActivatedAt);

public record ActiveLocaleResponse(
    string LocaleCode,
    string Name,
    string NativeName);

public record CreateLanguageResponse(string Id);

public record CreateLanguageRequest(
    string LanguageCode,
    string RegionCode,
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive);

public record UpdateLanguageRequest(
    string Name,
    string NativeName,
    bool IsRightToLeft,
    bool IsActive);

public record UpdateLanguageActivationRequest(bool IsActive);

public static class LanguageResponseMapper
{
    public static ActiveLocaleResponse ToActiveLocaleResponse(this LanguageReadModel model)
    {
        return new ActiveLocaleResponse(
            model.LocaleCode,
            model.Name,
            model.NativeName);
    }

    public static LanguageResponse ToResponse(this LanguageReadModel model)
    {
        return new LanguageResponse(
            model.Id,
            model.LocaleCode,
            model.LanguageCode,
            model.RegionCode,
            model.Name,
            model.NativeName,
            model.IsRightToLeft,
            model.IsActive,
            model.CreatedAt,
            model.UpdatedAt,
            model.LastActivatedAt);
    }
}
