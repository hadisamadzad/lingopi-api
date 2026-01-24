using Lingopi.Blog.Application.Interfaces;
using Lingopi.Blog.Application.Interfaces.Repositories;
using Lingopi.Blog.Infrastructure.Database.Repositories;
using MongoDB.Driver;

namespace Lingopi.Blog.Infrastructure.Database;

public class RepositoryManager(IMongoDatabase mongoDatabase) : IRepositoryManager
{
    public IArticleRepository Articles { get; } =
        new ArticleRepository(mongoDatabase, "blog.articles");

    public ITagRepository Tags { get; } =
        new TagRepository(mongoDatabase, "blog.tags");

    public ISubscriberRepository Subscribers { get; } =
        new SubscriberRepository(mongoDatabase, "blog.subscribers");

    public ISettingRepository Settings { get; } =
        new SettingRepository(mongoDatabase, "blog.settings");
}
