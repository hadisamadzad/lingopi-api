using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;

namespace Lingopi.Lingo.Application.Interfaces.Repositories;

public interface ILingoRepository : IRepository<LingoEntity>
{
    Task<List<LingoEntity>> GetByUserIdAsync(string userId);
}