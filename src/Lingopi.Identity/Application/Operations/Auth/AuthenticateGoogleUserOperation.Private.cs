using System.Security.Cryptography;
using System.Text;

namespace Lingopi.Identity.Application.Operations.Auth;

public partial class AuthenticateGoogleUserOperation
{
    private bool IsAuthorized(string providedSecret)
    {
        var expectedSecret = configuration["InternalAuthSecret"];
        if (string.IsNullOrEmpty(expectedSecret))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expectedSecret);
        var providedBytes = Encoding.UTF8.GetBytes(providedSecret);
        return expectedBytes.Length == providedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
