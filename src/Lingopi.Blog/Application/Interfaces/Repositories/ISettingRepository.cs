using Lingopi.Blog.Application.Types.Entities;
using Lingopi.Core.Interfaces;

namespace Lingopi.Blog.Application.Interfaces.Repositories;

public interface ISettingRepository : IRepository<SettingEntity>
{
    Task<SettingEntity> GetBlogSettingAsync();
}