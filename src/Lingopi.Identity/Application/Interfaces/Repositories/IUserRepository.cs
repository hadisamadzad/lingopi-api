using Lingopi.Core.Interfaces;
using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Interfaces.Repositories;

public interface IUserRepository : IRepository<UserEntity>
{
    Task<bool> AnyAsync();
    Task<UserEntity> GetByIdAsync(string id);
    Task<UserEntity> GetByEmailAsync(string email);
}
