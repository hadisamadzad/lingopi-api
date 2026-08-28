namespace Lingopi.Lingo.Api.Models;

public record UserSettingsResponse(
    string UserId,
    string TargetLocaleCode,
    List<string> SourceLocaleCodes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SaveUserSettingsRequest(
    string TargetLocaleCode,
    List<string> SourceLocaleCodes);

public static class UserSettingsResponseMapper
{
    public static UserSettingsResponse ToResponse(
        this Application.Models.ReadModels.UserSettingsModel model)
    {
        return new UserSettingsResponse(
            model.UserId,
            model.TargetLocaleCode,
            model.SourceLocaleCodes,
            model.CreatedAt,
            model.UpdatedAt);
    }
}
