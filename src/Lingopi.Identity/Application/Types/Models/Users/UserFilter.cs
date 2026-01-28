using Lingopi.Core.Utilities.Pagination;
using Lingopi.Identity.Application.Types.Entities;

namespace Lingopi.Identity.Application.Types.Models.Users;

public record UserFilter : PaginationFilter
{
    public required string Keyword { get; init; }
    public required string Email { get; init; }
    public required List<UserState> States { get; init; }

    public UserSortBy? SortBy { get; init; }
}
