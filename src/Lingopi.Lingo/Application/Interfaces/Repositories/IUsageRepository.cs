using Lingopi.Core.Interfaces;
using Lingopi.Lingo.Application.Models.Entities;
using Lingopi.Lingo.Application.Models.ReadModels;

namespace Lingopi.Lingo.Application.Interfaces.Repositories;

public interface IUsageRepository : IRepository<UsageRecordEntity>
{
    Task<UsageSummary> GetSummaryAsync(
        string userId,
        DateTime? periodStart = null,
        DateTime? periodEnd = null);

    Task<bool> RecordAsync(UsageRecordEntity record);
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}
