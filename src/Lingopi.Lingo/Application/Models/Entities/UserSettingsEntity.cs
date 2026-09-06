using Lingopi.Core.Interfaces;

namespace Lingopi.Lingo.Application.Models.Entities;

public class UserSettingsEntity : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TargetLocaleCode { get; set; } = string.Empty;
    public List<string> SourceLocaleCodes { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
