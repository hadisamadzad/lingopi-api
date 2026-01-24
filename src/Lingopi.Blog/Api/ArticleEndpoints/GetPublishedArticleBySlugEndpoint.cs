using Lingopi.Blog.Api.TagEndpoints;
using Lingopi.Blog.Application.Interfaces;
using Lingopi.Blog.Application.Operations.Articles;
using Lingopi.Blog.Application.Types.Entities;
using Lingopi.Core.Interfaces;
using Lingopi.Core.Utilities.OperationResult;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Blog.Api.ArticleEndpoints;

public class GetPublishedArticleBySlugEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        // Endpoint for getting an article by slug
        app.MapGroup(Routes.ArticleBaseRoute)
            .WithSummary("Get a Public Published Article by Slug")
            .MapGet("published/{slug}/", async (IOperationService operations,
                [FromRoute] string slug) =>
            {
                // Operation
                var operationResult = await operations.GetArticleBySlug
                    .ExecuteAsync(new GetPublishedArticleBySlugCommand(slug));

                // Result
                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new GetArticleResponse(
                            ArticleId: operationResult.Value!.Id,
                            AuthorId: operationResult.Value!.AuthorId,
                            Title: operationResult.Value!.Title,
                            Subtitle: operationResult.Value!.Subtitle,
                            Summary: operationResult.Value!.Summary,
                            Content: operationResult.Value!.Content,
                            Slug: operationResult.Value!.Slug,
                            ThumbnailUrl: operationResult.Value!.ThumbnailUrl,
                            CoverImageUrl: operationResult.Value!.CoverImageUrl,
                            TimeToReadInMinute: operationResult.Value!.TimeToReadInMinute,
                            Likes: operationResult.Value!.Likes,
                            Views: operationResult.Value!.Views,
                            Tags: [.. operationResult.Value!.Tags.Select(tag => new TagResponse(tag.TagId, tag.Name, tag.Slug))],
                            OriginalArticleInfo: operationResult.Value!.OriginalArticleInfo,
                            Status: operationResult.Value!.Status,
                            CreatedAt: operationResult.Value!.CreatedAt,
                            UpdatedAt: operationResult.Value!.UpdatedAt,
                            PublishedAt: operationResult.Value!.PublishedAt,
                            ArchivedAt: operationResult.Value!.ArchivedAt
                        )),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error),
                    OperationStatus.NotFound => Results.UnprocessableEntity(operationResult.Error),
                    _ => Results.InternalServerError(operationResult.Error),
                };
            })
            .WithTags(Routes.ArticleEndpointGroupTag)
            .WithDescription("Gets an article by its slug.")
            .Produces<GetArticleResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}

public record GetArticleResponse(
    string ArticleId,
    string AuthorId,
    string Title,
    string Subtitle,
    string Summary,
    string Content,
    string Slug,
    string ThumbnailUrl,
    string CoverImageUrl,
    int TimeToReadInMinute,
    int Likes,
    int Views,
    ICollection<TagResponse> Tags,
    OriginalArticleInfoValue? OriginalArticleInfo,
    ArticleStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt,
    DateTime? ArchivedAt);