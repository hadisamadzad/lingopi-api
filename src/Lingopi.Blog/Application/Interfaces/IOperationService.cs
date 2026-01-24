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

namespace Lingopi.Blog.Application.Interfaces;

public interface IOperationService
{
    // Article
    IOperation<CreateArticleCommand, string> CreateArticle { get; }
    IOperation<DeleteArticleCommand, NoResult> DeleteArticle { get; }
    IOperation<GetArticleByIdCommand, ArticleModel> GetArticleById { get; }
    IOperation<GetPublishedArticleBySlugCommand, ArticleModel> GetArticleBySlug { get; }
    IOperation<GetArticlesByFilterCommand, PaginatedList<ArticleModel>> GetArticlesByFilter { get; }
    IOperation<UpdateArticleCommand, NoResult> UpdateArticle { get; }
    IOperation<UpdateArticleStatusCommand, NoResult> UpdateArticleStatus { get; }
    IOperation<CountArticleViewCommand, NoResult> CountArticleView { get; }

    // Tags
    IOperation<CreateTagCommand, string> CreateTag { get; }
    IOperation<UpdateTagCommand, NoResult> UpdateTag { get; }
    IOperation<DeleteTagCommand, NoResult> DeleteTag { get; }
    IOperation<GetAllTagsCommand, List<TagModel>> GetAllTags { get; }

    // Settings
    IOperation<GetBlogSettingsCommand, SettingModel> GetBlogSettings { get; }
    IOperation<UpdateBlogSettingsCommand, NoResult> UpdateBlogSettings { get; }

    // Subscriber
    IOperation<CreateSubscriberCommand, string> CreateSubscriber { get; }
    IOperation<DeleteSubscriberCommand, NoResult> DeleteSubscriber { get; }
}