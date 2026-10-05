using Lingopi.Lingo.Api.Models;
using Lingopi.Lingo.Application.Operations.Lingos;
using Microsoft.AspNetCore.Mvc;

namespace Lingopi.Lingo.Api.Endpoints.Lingos;

public class GetLingoByIdEndpoint : IEndpoint
{
    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("api/lingos/{lingoId}", async (
                [FromServices] IOperationMediator operations,
                [FromHeader(Name = "User-Id")] string userId,
                [FromRoute] string lingoId) =>
            {
                var operationResult = await operations.ExecuteAsync(
                    new GetLingoByIdCommand(
                        UserId: userId,
                        LingoId: lingoId));
                var lingo = operationResult.Value!;

                return operationResult.Status switch
                {
                    OperationStatus.Completed => Results.Ok(
                        new LingoResponse(
                            LingoId: lingo.Id,
                            UserId: lingo.UserId,
                            Encounters: lingo.Encounters.ConvertAll(encounter =>
                                new EncounterResponse(
                                    OriginalText: encounter.OriginalText,
                                    SourceLanguageCode: encounter.SourceLanguageCode,
                                    SourceLocaleCode: encounter.SourceLocaleCode,
                                    Context: encounter.Context)),
                            Lingo: new LingoDataResponse(
                                Expression: lingo.Lingo.Expression,
                                SourceLanguageCode: lingo.Lingo.SourceLanguageCode,
                                SourceLocaleCodes: lingo.Lingo.SourceLocaleCodes,
                                TargetLocaleCode: lingo.Lingo.TargetLocaleCode,
                                Pattern: lingo.Lingo.Pattern,
                                SenseKey: lingo.Lingo.SenseKey,
                                Type: lingo.Lingo.Type,
                                Registers: lingo.Lingo.Registers,
                                Domains: lingo.Lingo.Domains,
                                IsOffensive: lingo.Lingo.IsOffensive,
                                Definition: lingo.Lingo.Definition,
                                Translation: lingo.Lingo.Translation,
                                Note: lingo.Lingo.Note,
                                Examples: lingo.Lingo.Examples.ConvertAll(example =>
                                    new ExampleResponse(
                                        Text: example.Text,
                                        Translation: example.Translation)),
                                CommonMistakes: lingo.Lingo.CommonMistakes,
                                Tags: lingo.Lingo.Tags),
                            Learning: new LearningResponse(
                                Goal: lingo.Learning.Goal,
                                Status: lingo.Learning.Status,
                                CurrentReviewState: lingo.Learning.CurrentReviewState,
                                Review: new SrsReviewResponse(
                                    LastReviewedAt: lingo.Learning.Review.LastReviewedAt,
                                    NextReviewAt: lingo.Learning.Review.NextReviewAt,
                                    Repetitions: lingo.Learning.Review.Repetitions,
                                    Level: lingo.Learning.Review.Level)),
                            Enrichment: new EnrichmentResponse(
                                Status: lingo.Enrichment.Status,
                                EnrichmentJobId: lingo.Enrichment.EnrichmentJobId,
                                LastEnrichedAt: lingo.Enrichment.LastEnrichedAt,
                                Provider: lingo.Enrichment.Provider,
                                Model: lingo.Enrichment.Model,
                                PromptVersion: lingo.Enrichment.PromptVersion,
                                ErrorCode: lingo.Enrichment.ErrorCode,
                                ErrorMessage: lingo.Enrichment.ErrorMessage),
                            Audit: new AuditResponse(
                                CreatedAt: lingo.Audit.CreatedAt,
                                UpdatedAt: lingo.Audit.UpdatedAt,
                                DocumentRevision: lingo.Audit.DocumentRevision,
                                SchemaVersion: lingo.Audit.SchemaVersion))),
                    OperationStatus.Invalid => Results.BadRequest(operationResult.Error?.Messages),
                    OperationStatus.NotFound => Results.UnprocessableEntity(operationResult.Error?.Messages),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: operationResult.Error?.Messages?.FirstOrDefault() ??
                            "An unexpected error occurred while retrieving the lingo.")
                };
            })
            .WithTags("Lingos")
            .WithSummary("Get a lingo by ID")
            .WithName("GetLingoById")
            .WithDescription("Get a specific lingo item by its unique ID")
            .Produces<LingoResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
