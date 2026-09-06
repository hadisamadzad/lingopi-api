using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Lingo.Application.Interfaces.Repositories;
using Lingopi.Lingo.Application.Models.Entities;
using MongoDB.Driver;

namespace Lingopi.Lingo.Infrastructure.Database.Repositories;

public sealed class UserSettingsRepository(IMongoDatabase database) :
    MongoDbRepositoryBase<UserSettingsEntity>(database, "lingo.user-settings"),
    IUserSettingsRepository
{
    public async Task<UserSettingsEntity?> GetByUserIdAsync(string userId)
    {
        return await _collection
            .Find(settings => settings.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpsertAsync(UserSettingsEntity settings)
    {
        var result = await _collection.ReplaceOneAsync(
            item => item.UserId == settings.UserId,
            settings,
            new ReplaceOptions { IsUpsert = true });

        return result.IsAcknowledged &&
            (result.MatchedCount == 1 || result.UpsertedId is not null);
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await _collection.Indexes.CreateOneAsync(
            new CreateIndexModel<UserSettingsEntity>(
                Builders<UserSettingsEntity>.IndexKeys.Ascending(settings => settings.UserId),
                new CreateIndexOptions
                {
                    Name = "user_settings_user_id",
                    Unique = true
                }),
            cancellationToken: cancellationToken);
    }
}
