using FluentAssertions;
using Moq;
using Dabp.Utils.Security;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Contracts.Services;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

public class UserCredentialServiceTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    private UserCredentialService CreateService() =>
        new(_userService.Object, _passwordHasher.Object);

    [Fact]
    public async Task VerifyUserPasswordAsync_UserNotFound_ReturnsUserNotFound()
    {
        _userService
            .Setup(x => x.GetUserByIdAsync(1))
            .ReturnsAsync((User?)null);
        var service = CreateService();

        var result = await service.VerifyUserPasswordAsync(1, "any-password");

        result.Should().Be(PasswordVerificationResult.UserNotFound, "用户不存在时不应触发哈希比较");
        _passwordHasher.Verify(
            x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "用户不存在时不应比较密码哈希");
    }

    [Fact]
    public async Task VerifyUserPasswordAsync_EmptyHash_ReturnsNoPasswordHash()
    {
        _userService
            .Setup(x => x.GetUserByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, PasswordHash = "" });
        var service = CreateService();

        var result = await service.VerifyUserPasswordAsync(1, "any-password");

        result.Should().Be(PasswordVerificationResult.NoPasswordHash, "未设置哈希的用户无法通过校验");
        _passwordHasher.Verify(
            x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "缺失哈希时不应进入比较逻辑");
    }

    [Fact]
    public async Task VerifyUserPasswordAsync_HashMismatch_ReturnsMismatch()
    {
        _userService
            .Setup(x => x.GetUserByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, PasswordHash = "stored-hash" });
        _passwordHasher
            .Setup(x => x.VerifyPassword("wrong-password", "stored-hash"))
            .Returns(false);
        var service = CreateService();

        var result = await service.VerifyUserPasswordAsync(1, "wrong-password");

        result.Should().Be(PasswordVerificationResult.Mismatch, "密码不匹配应返回 Mismatch");
    }

    [Fact]
    public async Task VerifyUserPasswordAsync_ValidPassword_ReturnsSuccess()
    {
        _userService
            .Setup(x => x.GetUserByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, PasswordHash = "stored-hash" });
        _passwordHasher
            .Setup(x => x.VerifyPassword("correct-password", "stored-hash"))
            .Returns(true);
        var service = CreateService();

        var result = await service.VerifyUserPasswordAsync(1, "correct-password");

        result.Should().Be(PasswordVerificationResult.Success, "密码正确应返回 Success");
    }
}
