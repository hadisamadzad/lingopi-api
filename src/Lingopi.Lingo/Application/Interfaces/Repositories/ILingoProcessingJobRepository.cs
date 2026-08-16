using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;

namespace Lingopi.Lingo.Application.Interfaces.Repositories;

public interface ILingoProcessingJobRepository : IRepository<LingoProcessingJobEntity>
{
    Task<LingoProcessingJobEntity?> GetByIdAsync(string jobId);
    Task<List<LingoProcessingJobEntity>> GetByLingoIdAsync(string lingoId);
}
