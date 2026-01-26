using Lingopi.Lingo.Application.Operations.Lingos;

namespace Lingopi.Lingo.Application.Interfaces;

public interface IOperationService
{
    CreateLingoOperation CreateLingo { get; }
    GetLingosByUserIdOperation GetLingosByUserId { get; }
}