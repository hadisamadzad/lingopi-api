using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Lingopi.Identity.Application.Helpers;

public static partial class PasswordHelper
{
    private const int SaltSize = 128 / 8; // 128 bit
    private const int KeySize = 256 / 8; // 256 bit
    private const int Iteration = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iteration,
            HashAlgorithmName.SHA512,
            KeySize);

        return $"{Convert.ToBase64String(key)}.{Convert.ToBase64String(salt)}";
    }

    public static bool CheckPasswordHash(string hash, string password)
    {
        var parts = hash.Split('.', 2);

        if (parts.Length != 2)
        {
            throw new FormatException("Unexpected hash format. " +
              "Should be formatted as `{hash}.{salt}`");
        }

        var key = Convert.FromBase64String(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);

        var keyToCheck = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iteration,
            HashAlgorithmName.SHA512,
            KeySize);
        return CryptographicOperations.FixedTimeEquals(keyToCheck, key);
    }

    public static PasswordScore CheckStrength(string password)
    {
        if (password.Length < 1)
        {
            return PasswordScore.Blank;
        }

        if (password.Length < 6)
        {
            return PasswordScore.VeryWeak;
        }

        var score = 0;

        if (password.Length >= 8)
        {
            score++;
        }

        if (password.Length >= 12)
        {
            score++;
        }

        if (NumberRegex().Match(password).Success)
        {
            score++;
        }

        if (LowercaseRegex().IsMatch(password) && UppercaseRegex().IsMatch(password))
        {
            score++;
        }

        if (SpecialCharRegex().IsMatch(password))
        {
            score++;
        }

        return (PasswordScore)score;
    }

    [GeneratedRegex(@"\d")]
    private static partial Regex NumberRegex();
    [GeneratedRegex("[a-z]")]
    private static partial Regex LowercaseRegex();
    [GeneratedRegex("[A-Z]")]
    private static partial Regex UppercaseRegex();
    [GeneratedRegex(".[~,!,@,#,$,%,^,&,*,(,),-,_,=,?,_]")]
    private static partial Regex SpecialCharRegex();
}

public enum PasswordScore
{
    Blank = 0,
    VeryWeak = 1,
    Weak = 2,
    Medium = 3,
    Strong = 4,
    VeryStrong = 5
}
