using Lingopi.Lingo.Application.Interfaces.Repositories;

namespace Lingopi.Lingo.Application.Interfaces;

public interface IRepositoryManager
{
    ILingoRepository Lingos { get; }
    ILanguageRepository Languages { get; }
}
