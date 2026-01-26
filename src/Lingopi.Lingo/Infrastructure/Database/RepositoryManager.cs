using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Interfaces.Repositories;
using Lingopi.Lingo.Infrastructure.Database.Repositories;
using MongoDB.Driver;

namespace Lingopi.Lingo.Infrastructure.Database;

public class RepositoryManager(IMongoDatabase database) : IRepositoryManager
{
    public ILingoRepository Lingos { get; } = new LingoRepository(database);
}
