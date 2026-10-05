using Lingopi.Identity.Application.Types.Entities;
using Lingopi.Identity.Application.Types.Models.Users;

namespace Lingopi.Identity.Application.Interfaces.Repositories;

public interface IUserRepository : IRepository<UserEntity>
{
    Task<bool> AnyAsync();
    Task<UserEntity?> GetByIdAsync(string id);
    Task<UserEntity?> GetByEmailAsync(string email);
    Task<List<UserEntity>> GetByFilterAsync(UserFilter filter);
    Task<long> CountByFilterAsync(UserFilter filter);
}
