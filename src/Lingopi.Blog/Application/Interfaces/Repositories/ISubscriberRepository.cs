using Lingopi.Blog.Application.Types.Entities;
using Lingopi.Core.Interfaces;

namespace Lingopi.Blog.Application.Interfaces.Repositories;

public interface ISubscriberRepository : IRepository<SubscriberEntity>
{
    Task<SubscriberEntity> GetByEmailAsync(string email);
}