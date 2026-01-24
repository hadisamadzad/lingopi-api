using Lingopi.Blog.Application.Interfaces.Repositories;

namespace Lingopi.Blog.Application.Interfaces;

public interface IRepositoryManager
{
    IArticleRepository Articles { get; }
    ITagRepository Tags { get; }
    ISubscriberRepository Subscribers { get; }
    ISettingRepository Settings { get; }
}
