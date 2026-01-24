using Lingopi.Blog.Application.Interfaces;
using Lingopi.Blog.Application.Types.Models.Settings;
using Lingopi.Core.Utilities.OperationResult;

namespace Lingopi.Blog.Application.Operations.Settings;

public class GetBlogSettingsOperation(IRepositoryManager repository) :
    IOperation<GetBlogSettingsCommand, SettingModel>
{
    public async Task<OperationResult<SettingModel>> ExecuteAsync(
        GetBlogSettingsCommand command, CancellationToken? cancellation = null)
    {
        // Retrieve the article
        var entity = await repository.Settings.GetBlogSettingAsync();
        if (entity is null)
            return OperationResult<SettingModel>.NotFoundFailure("Blog settings not found.");

        return OperationResult<SettingModel>.Success(entity.MapToModel());
    }
}

public record GetBlogSettingsCommand() : IOperationCommand;