using Lingopi.Core.Persistence.MongoDB;
using Lingopi.Lingo.Application.Interfaces.Repositories;
using Lingopi.Lingo.Application.Models.Entities;
using MongoDB.Driver;

namespace Lingopi.Lingo.Infrastructure.Database.Repositories;

public class LingoProcessingJobRepository(IMongoDatabase database) :
    MongoDbRepositoryBase<LingoProcessingJobEntity>(database, "lingo.processingJobs"),
    ILingoProcessingJobRepository
{
    public async Task<LingoProcessingJobEntity?> GetByIdAsync(string jobId)
    {
        return await _collection
            .Find(job => job.Id == jobId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<LingoProcessingJobEntity>> GetByLingoIdAsync(string lingoId)
    {
        return await _collection
            .Find(job => job.LingoId == lingoId)
            .ToListAsync();
    }
}
