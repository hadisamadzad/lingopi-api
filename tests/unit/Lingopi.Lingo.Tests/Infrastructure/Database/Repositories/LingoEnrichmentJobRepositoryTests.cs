#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.Enums;
using Lingopi.Lingo.Application.Models.Filters;
using Lingopi.Lingo.Infrastructure.Database.Repositories;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace Lingopi.Lingo.Tests.Infrastructure.Database.Repositories;

public class LingoEnrichmentJobRepositoryTests
{
    [Fact]
    public async Task ClaimNextAsync_ShouldUseAtomicFindOneAndUpdate()
    {
        var collection = Substitute.For<IMongoCollection<EnrichmentJobEntity>>();
        var database = Substitute.For<IMongoDatabase>();
        database.GetCollection<EnrichmentJobEntity>("lingo.enrichment-jobs", Arg.Any<MongoCollectionSettings?>())
            .Returns(collection);

        var claimedJob = new EnrichmentJobEntity { Id = "lingo-job-1" };
        collection.FindOneAndUpdateAsync(
                Arg.Any<FilterDefinition<EnrichmentJobEntity>>(),
                Arg.Any<UpdateDefinition<EnrichmentJobEntity>>(),
                Arg.Any<FindOneAndUpdateOptions<EnrichmentJobEntity>>(),
                Arg.Any<CancellationToken>())
            .Returns(claimedJob);

        var repository = new EnrichmentJobRepository(database);
        var filter = new EnrichmentJobClaimFilter(
            new DateTime(2026, 08, 16, 22, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 08, 16, 21, 55, 0, DateTimeKind.Utc),
            3);

        var result = await repository.ClaimNextAndUpdateAsync(filter, CancellationToken.None);

        Assert.Same(claimedJob, result);
        await collection.Received(1).FindOneAndUpdateAsync(
            Arg.Any<FilterDefinition<EnrichmentJobEntity>>(),
            Arg.Any<UpdateDefinition<EnrichmentJobEntity>>(),
            Arg.Is<FindOneAndUpdateOptions<EnrichmentJobEntity>>(options =>
                options.ReturnDocument == ReturnDocument.After),
            Arg.Any<CancellationToken>());
    }
}
