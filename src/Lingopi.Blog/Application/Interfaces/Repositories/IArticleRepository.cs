using Lingopi.Blog.Application.Types.Entities;
using Lingopi.Blog.Application.Types.Models.Articles;
using Lingopi.Core.Interfaces;

namespace Lingopi.Blog.Application.Interfaces.Repositories;

public interface IArticleRepository : IRepository<ArticleEntity>
{
    Task<ArticleEntity> GetByIdAsync(string id);
    Task<ArticleEntity> GetBySlugAsync(string slug);
    Task<ArticleEntity> GetPublishedBySlugAsync(string slug);
    Task<List<ArticleEntity>> GetByIdsAsync(IEnumerable<string> ids);
    Task<List<ArticleEntity>> GetByFilterAsync(ArticleFilter filter);
    Task<int> CountByFilterAsync(ArticleFilter filter);
    Task<bool> IncrementViewsAsync(string articleId, long delta);
}