using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Repositories;
using Lingopi.Lingo.Infrastructure.Database.Repositories;
using MongoDB.Driver;

namespace Lingopi.Lingo.Infrastructure.Database;

public class RepositoryManager(IMongoDatabase database) : IRepositoryManager
{
    public ICaptureRepository Captures { get; } = new CaptureRepository(database);
    public ILingoRepository Lingos { get; } = new LingoRepository(database);
    public IEnrichmentJobRepository EnrichmentJobs { get; } = new EnrichmentJobRepository(database);
    public IUserSettingsRepository UserSettings { get; } = new UserSettingsRepository(database);
    public ISubscriptionRepository Subscriptions { get; } = new SubscriptionRepository(database);
    public IUsageRepository Usage { get; } = new UsageRepository(database);
    public ILanguageRepository Languages { get; } = new LanguageRepository(database);
}
