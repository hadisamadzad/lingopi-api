namespace Lingopi.Lingo.Application.Models.Entities;

public class LanguageEntity : IEntity
{
    public required string Id { get; set; }

    public required string LocaleCode { get; set; }

    public required string LanguageCode { get; set; }

    public required string RegionCode { get; set; }

    public required string Name { get; set; }

    public required string NativeName { get; set; }

    public bool IsRightToLeft { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? LastActivatedAt { get; set; }
}
