using FluentValidation.Results;

namespace Lingopi.Core.Extensions;

public static class ValidationResultExtensions
{
    public static IEnumerable<string> GetErrorMessages(this ValidationResult result)
    {
        return result.Errors.Select(x => x.ErrorMessage);
    }
}
