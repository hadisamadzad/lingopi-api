using Lingopi.Identity.Application.Interfaces.Repositories;

namespace Lingopi.Identity.Application.Interfaces;

public interface IRepositoryManager
{
    IUserRepository Users { get; }
}
