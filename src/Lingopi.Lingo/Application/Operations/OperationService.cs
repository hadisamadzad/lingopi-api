using Lingopi.Lingo.Application.Interfaces;
using Lingopi.Lingo.Application.Models.ReadModels;
using Lingopi.Lingo.Application.Operations.Lingos;
using Minimals.Operations;

namespace Lingopi.Lingo.Application.Operations;

public class OperationService(
    IOperation<CreateLingoCommand, string> createLingo,
    IOperation<GetLingoByIdCommand, LingoModel> getLingoById,
    IOperation<GetLingosByUserIdCommand, List<LingoModel>> getLingosByUserId
) : IOperationService
{
    public CreateLingoOperation CreateLingo { get; } =
        createLingo as CreateLingoOperation ?? throw new ArgumentNullException(nameof(createLingo));

    public GetLingoByIdOperation GetLingoById { get; } =
        getLingoById as GetLingoByIdOperation ?? throw new ArgumentNullException(nameof(getLingoById));

    public GetLingosByUserIdOperation GetLingosByUserId { get; } =
        getLingosByUserId as GetLingosByUserIdOperation ?? throw new ArgumentNullException(nameof(getLingosByUserId));
}
