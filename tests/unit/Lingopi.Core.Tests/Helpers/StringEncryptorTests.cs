using System.Security.Cryptography;
using Lingopi.Core.Helpers;
using Xunit;

namespace Lingopi.Core.Tests.Helpers;

public class StringEncryptorTests
{
    private const string Key = "1234567890123456"; // 16 bytes for AES-128

    [Fact]
    public void TestEncrypt_WhenPlainTextProvided_ShouldReturnCipherText()
    {
        // Arrange
        string plainText = "Hello, Lingopi!";

        // Act
        var encrypted = StringEncryptor.Encrypt(plainText, Key);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(encrypted));
    }

    [Fact]
    public void TestDecrypt_WhenValidCipherTextProvided_ShouldReturnOriginalPlainText()
    {
        // Arrange
        string plainText = "Hello, Lingopi!";
        var encrypted = StringEncryptor.Encrypt(plainText, Key);

        // Act
        var decrypted = StringEncryptor.Decrypt(encrypted, Key);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void TestDecrypt_WhenKeyIsWrong_ShouldThrowException()
    {
        // Arrange
        string plainText = "Hello, Lingopi!";
        var encrypted = StringEncryptor.Encrypt(plainText, Key);
        string wrongKey = "6543210987654321";

        // Assert
        Assert.Throws<CryptographicException>(
            () => StringEncryptor.Decrypt(encrypted, wrongKey));
    }
}