using Lingopi.Lingo.Application.Models.ValueObjects;

namespace Lingopi.Lingo.Application.Models.Entities;

public class LanguageEntity : IEntity
{
    public required string Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string NativeName { get; set; }
    public bool IsActive { get; set; }
    public List<LocaleValue> Locales { get; set; } = [];
}
