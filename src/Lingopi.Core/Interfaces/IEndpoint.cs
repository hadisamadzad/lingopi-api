using Microsoft.AspNetCore.Builder;

namespace Lingopi.Core.Interfaces;

public interface IEndpoint
{
    void MapEndpoints(WebApplication app);
}