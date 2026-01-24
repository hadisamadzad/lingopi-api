using Lingopi.Blog.Application.Types.Entities;
using Lingopi.Core.Interfaces;

namespace Lingopi.Blog.Application.Interfaces.Repositories;

public interface ITagRepository : IRepository<TagEntity>
{
    Task<TagEntity> GetByIdAsync(string id);
    Task<List<TagEntity>> GetByIdsAsync(IEnumerable<string> ids);
    Task<TagEntity> GetBySlugAsync(string slug);
    Task<List<TagEntity>> GetAllAsync();
}