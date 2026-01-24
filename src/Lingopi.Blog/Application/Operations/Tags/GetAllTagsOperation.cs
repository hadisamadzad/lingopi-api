using Lingopi.Blog.Application.Interfaces;
using Lingopi.Blog.Application.Types.Models.Tags;
using Lingopi.Core.Utilities.OperationResult;

namespace Lingopi.Blog.Application.Operations.Tags;

public class GetAllTagsOperation(IRepositoryManager repository) :
    IOperation<GetAllTagsCommand, List<TagModel>>
{
    public async Task<OperationResult<List<TagModel>>> ExecuteAsync(GetAllTagsCommand command,
        CancellationToken? cancellation = null)
    {
        var tags = await repository.Tags.GetAllAsync();

        return OperationResult<List<TagModel>>.Success([.. tags.MapToModels()]);
    }
}

public record GetAllTagsCommand : IOperationCommand;
