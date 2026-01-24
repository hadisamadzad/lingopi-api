using Lingopi.Blog.Application.Interfaces;
using Lingopi.Blog.Application.Operations.Articles;
using Lingopi.Blog.Application.Operations.Settings;
using Lingopi.Blog.Application.Operations.Subscribers;
using Lingopi.Blog.Application.Operations.Tags;
using Lingopi.Blog.Application.Operations.Views;
using Lingopi.Blog.Application.Types.Models.Articles;
using Lingopi.Blog.Application.Types.Models.Settings;
using Lingopi.Blog.Application.Types.Models.Tags;
using Lingopi.Core.Utilities.OperationResult;
using Lingopi.Core.Utilities.Pagination;

namespace Lingopi.Blog.Application.Operations;

#pragma warning disable S107 // Avoid excessive complexity
public class OperationService(
    // Articles
    IOperation<CreateArticleCommand, string> createArticle,
    IOperation<DeleteArticleCommand, NoResult> deleteArticle,
    IOperation<GetArticleByIdCommand, ArticleModel> getArticleById,
    IOperation<GetPublishedArticleBySlugCommand, ArticleModel> getArticleBySlug,
    IOperation<GetArticlesByFilterCommand, PaginatedList<ArticleModel>> getArticlesByFilter,
    IOperation<UpdateArticleCommand, NoResult> updateArticle,
    IOperation<UpdateArticleStatusCommand, NoResult> updateArticleStatus,
    IOperation<CountArticleViewCommand, NoResult> countArticleView,

    // Tags
    IOperation<CreateTagCommand, string> createTag,
    IOperation<UpdateTagCommand, NoResult> updateTag,
    IOperation<DeleteTagCommand, NoResult> deleteTag,
    IOperation<GetAllTagsCommand, List<TagModel>> getAllTags,

    // Settings
    IOperation<GetBlogSettingsCommand, SettingModel> getBlogSettings,
    IOperation<UpdateBlogSettingsCommand, NoResult> updateBlogSettings,

    // Subscribers
    IOperation<CreateSubscriberCommand, string> createSubscriber,
    IOperation<DeleteSubscriberCommand, NoResult> deleteSubscriber
    ) : IOperationService
#pragma warning restore S107
{
    // Article
    public IOperation<CreateArticleCommand, string> CreateArticle { get; } = createArticle;
    public IOperation<DeleteArticleCommand, NoResult> DeleteArticle { get; } = deleteArticle;
    public IOperation<GetArticleByIdCommand, ArticleModel> GetArticleById { get; } = getArticleById;
    public IOperation<GetPublishedArticleBySlugCommand, ArticleModel> GetArticleBySlug { get; } = getArticleBySlug;
    public IOperation<GetArticlesByFilterCommand, PaginatedList<ArticleModel>> GetArticlesByFilter { get; } = getArticlesByFilter;
    public IOperation<UpdateArticleCommand, NoResult> UpdateArticle { get; } = updateArticle;
    public IOperation<UpdateArticleStatusCommand, NoResult> UpdateArticleStatus { get; } = updateArticleStatus;
    public IOperation<CountArticleViewCommand, NoResult> CountArticleView { get; } = countArticleView;

    // Tags
    public IOperation<CreateTagCommand, string> CreateTag { get; } = createTag;
    public IOperation<UpdateTagCommand, NoResult> UpdateTag { get; } = updateTag;
    public IOperation<DeleteTagCommand, NoResult> DeleteTag { get; } = deleteTag;
    public IOperation<GetAllTagsCommand, List<TagModel>> GetAllTags { get; } = getAllTags;

    // Settings
    public IOperation<GetBlogSettingsCommand, SettingModel> GetBlogSettings { get; } = getBlogSettings;
    public IOperation<UpdateBlogSettingsCommand, NoResult> UpdateBlogSettings { get; } = updateBlogSettings;

    // Subscriber
    public IOperation<CreateSubscriberCommand, string> CreateSubscriber { get; } = createSubscriber;
    public IOperation<DeleteSubscriberCommand, NoResult> DeleteSubscriber { get; } = deleteSubscriber;

}