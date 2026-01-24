using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Interfaces.Repositories;
using Lingopi.Identity.Infrastructure.Database.Repositories;
using MongoDB.Driver;

namespace Lingopi.Identity.Infrastructure.Database;

public class RepositoryManager(IMongoDatabase mongoDatabase) : IRepositoryManager
{
    public IUserRepository Users { get; } =
        new UserRepository(mongoDatabase, "identity.users");
}
