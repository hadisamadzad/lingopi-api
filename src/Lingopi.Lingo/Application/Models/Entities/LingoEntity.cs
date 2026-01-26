using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.ValueObjects;

namespace Lingopi.Lingo.Application.Models.Entities;

public class LingoEntity : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Lingo { get; set; } = string.Empty;
    public LingoType LingoType { get; set; }
    public string Definition { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public WordStyle? Style { get; set; }
    public List<string> Examples { get; set; } = [];
    public List<Context> Context { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public LearningGoal? LearningGoal { get; set; }
    public string? UserNote { get; set; }
    public required LanguagesValue Languages { get; set; }
    public ReviewValue Review { get; set; } = new();
    public SourceValue? Source { get; set; }
    public AuditValue Audit { get; set; } = new();
}

public record LanguagesValue
{
    public required LanguageValue Source { get; init; }
    public required LanguageValue Target { get; init; }
}

public record ReviewValue
{
    public DateTime? LastTime { get; set; }
    public DateTime? NextTime { get; set; }
    public int Repetitions { get; set; }
    public int SrsLevel { get; set; } = 1;
}

public record SourceValue
{
    public SourceMethod Method { get; init; }
    public string? Model { get; init; }
    public string? Version { get; init; }
}

public record AuditValue
{
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Version { get; init; } = 1;
}
