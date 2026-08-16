using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Identity.Application.Interfaces.Repositories;
using Lingopi.Identity.Application.Types.Entities;
using MongoDB.Driver;

namespace Lingopi.Identity.Infrastructure.Database.Repositories;

public class RefreshTokenRepository(IMongoDatabase database) :
    MongoDbRepositoryBase<RefreshTokenEntity>(database, "identity.refresh-tokens"), IRefreshTokenRepository
{
    public async Task<RefreshTokenEntity?> ConsumeAsync(string tokenHash, DateTime now, string replacementTokenId)
    {
        var update = Builders<RefreshTokenEntity>.Update
            .Set(x => x.RevokedAt, now)
            .Set(x => x.ReplacedByTokenId, replacementTokenId);

        return await _collection.FindOneAndUpdateAsync(x =>
                x.TokenHash == tokenHash && x.RevokedAt == null && x.ExpiresAt > now,
            update,
            new FindOneAndUpdateOptions<RefreshTokenEntity>
            {
                ReturnDocument = ReturnDocument.Before
            });
    }

    public async Task<bool> RevokeAsync(string tokenHash, DateTime now)
    {
        var result = await _collection.UpdateOneAsync(
            x => x.TokenHash == tokenHash && x.RevokedAt == null,
            Builders<RefreshTokenEntity>.Update.Set(x => x.RevokedAt, now));

        return result.IsAcknowledged && result.ModifiedCount == 1;
    }
}
