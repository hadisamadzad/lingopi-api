#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lingopi.Identity.Application.Helpers;
using Lingopi.Identity.Application.Interfaces;
using Lingopi.Identity.Application.Operations.PasswordReset;
using Lingopi.Identity.Application.Types.Configs;
using Lingopi.Identity.Application.Types.Entities;
using Microsoft.Extensions.Options;
using Minimals.Operations;
using NSubstitute;
using Xunit;

namespace Lingopi.Identity.Tests.Application.Operations.PasswordReset;

public sealed class PasswordResetOperationTests
{
    private const string Email = "user@example.com";
    private const string EncryptionKey = "0123456789abcdef0123456789abcdef";

    public PasswordResetOperationTests()
    {
        PasswordResetTokenHelper.SetEncryptionKey(EncryptionKey);
    }

    [Fact]
    public async Task GetPasswordResetEmail_WhenUserExists_ShouldReturnEmail()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByEmailAsync(Email).Returns(new UserEntity
        {
            Email = Email,
            Status = UserState.Active
        });
        var operation = new GetPasswordResetEmailOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetPasswordResetEmailCommand(CreateToken()),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal(Email, result.Value);
    }

    [Fact]
    public async Task GetPasswordResetEmail_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByEmailAsync(Email).Returns((UserEntity?)null);
        var operation = new GetPasswordResetEmailOperation(repository);

        var result = await operation.ExecuteAsync(
            new GetPasswordResetEmailCommand(CreateToken()),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task SendPasswordResetEmail_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByEmailAsync(Email).Returns((UserEntity?)null);
        var emailService = Substitute.For<IEmailService>();
        var operation = new SendPasswordResetEmailOperation(
            repository,
            emailService,
            Options.Create(CreateConfig()));

        var result = await operation.ExecuteAsync(
            new SendPasswordResetEmailCommand(Email),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
        await emailService.DidNotReceiveWithAnyArgs()
            .SendEmailByTemplateIdAsync(default, default!, default!);
    }

    [Fact]
    public async Task SendPasswordResetEmail_WhenUserIsActive_ShouldSendTemplateEmail()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByEmailAsync(Email).Returns(new UserEntity
        {
            Email = Email,
            Status = UserState.Active
        });
        var emailService = Substitute.For<IEmailService>();
        emailService.SendEmailByTemplateIdAsync(
                Arg.Any<long>(),
                Arg.Any<List<string>>(),
                Arg.Any<Dictionary<string, string>>())
            .Returns(true);
        var operation = new SendPasswordResetEmailOperation(
            repository,
            emailService,
            Options.Create(CreateConfig()));

        var result = await operation.ExecuteAsync(
            new SendPasswordResetEmailCommand(Email),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        await emailService.Received(1).SendEmailByTemplateIdAsync(
            42,
            Arg.Is<List<string>>(recipients => recipients.Count == 1 && recipients[0] == Email),
            Arg.Is<Dictionary<string, string>>(parameters =>
                parameters.ContainsKey("Link") &&
                parameters["Link"].StartsWith("https://example.test/reset/", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ResetPassword_WhenUserExists_ShouldUpdatePassword()
    {
        var repository = Substitute.For<IRepositoryManager>();
        var user = new UserEntity
        {
            Email = Email,
            Status = UserState.Active,
            PasswordHash = PasswordHelper.Hash("old-password")
        };
        repository.Users.GetByEmailAsync(Email).Returns(user);
        repository.Users.UpdateAsync(user).Returns(true);
        var operation = new ResetPasswordOperation(
            repository,
            Options.Create(CreateConfig()));

        var result = await operation.ExecuteAsync(
            new ResetPasswordCommand(CreateToken(), "NewPassword123!"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.True(PasswordHelper.CheckPasswordHash(user.PasswordHash, "NewPassword123!"));
        await repository.Users.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ResetPassword_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRepositoryManager>();
        repository.Users.GetByEmailAsync(Email).Returns((UserEntity?)null);
        var operation = new ResetPasswordOperation(
            repository,
            Options.Create(CreateConfig()));

        var result = await operation.ExecuteAsync(
            new ResetPasswordCommand(CreateToken(), "NewPassword123!"),
            CancellationToken.None);

        Assert.Equal(OperationStatus.NotFound, result.Status);
    }

    private static string CreateToken() =>
        PasswordResetTokenHelper.GeneratePasswordResetToken(
            Email,
            DateTime.UtcNow.AddDays(1));

    private static PasswordResetConfig CreateConfig() => new()
    {
        LinkFormat = "https://example.test/reset/{0}",
        LinkLifetimeInDays = 1,
        BrevoTemplateId = 42
    };
}
