using Lingopi.Lingo.Application.Interfaces.Repositories;

namespace Lingopi.Lingo.Application.Interfaces;

public interface IRepositoryManager
{
    ICaptureRepository Captures { get; }
    ILingoRepository Lingos { get; }
    IEnrichmentJobRepository EnrichmentJobs { get; }
    IUserSettingsRepository UserSettings { get; }
    IUsageRepository Usage { get; }
    ILanguageRepository Languages { get; }
}
