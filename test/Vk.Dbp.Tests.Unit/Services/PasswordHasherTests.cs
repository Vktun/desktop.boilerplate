using System;
using Dabp.Utils.Security;
using FluentAssertions;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _passwordHasher;
        
        public PasswordHasherTests()
        {
            _passwordHasher = new PasswordHasher();
        }
        
        [Fact]
        public void HashPassword_SameInput_ProducesDifferentHashes()
        {
            // Arrange
            string password = "MyTestPassword123!";
            
            // Act
            string hash1 = _passwordHasher.HashPassword(password);
            string hash2 = _passwordHasher.HashPassword(password);
            
            // Assert
            hash1.Should().NotBeNullOrEmpty();
            hash2.Should().NotBeNullOrEmpty();
            hash1.Should().NotBe(hash2, "相同的密码应该生成不同的哈希值（由于盐值）");
        }
        
        [Fact]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            // Arrange
            string password = "CorrectPassword123!";
            string hash = _passwordHasher.HashPassword(password);
            
            // Act
            bool result = _passwordHasher.VerifyPassword(password, hash);
            
            // Assert
            result.Should().BeTrue("正确的密码应该验证通过");
        }
        
        [Fact]
        public void VerifyPassword_WrongPassword_ReturnsFalse()
        {
            // Arrange
            string correctPassword = "CorrectPassword123!";
            string wrongPassword = "WrongPassword456!";
            string hash = _passwordHasher.HashPassword(correctPassword);
            
            // Act
            bool result = _passwordHasher.VerifyPassword(wrongPassword, hash);
            
            // Assert
            result.Should().BeFalse("错误的密码应该验证失败");
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void HashPassword_NullOrEmpty_ThrowsArgumentException(string password)
        {
            // Act
            Action act = () => _passwordHasher.HashPassword(password);
            
            // Assert
            act.Should().Throw<ArgumentException>("空或null密码应该抛出异常");
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void VerifyPassword_NullOrEmptyHash_ReturnsFalse(string hash)
        {
            // Arrange
            string password = "TestPassword123!";
            
            // Act
            bool result = _passwordHasher.VerifyPassword(password, hash);
            
            // Assert
            result.Should().BeFalse("空的哈希值应该返回false");
        }
        
        [Fact]
        public void HashPassword_DifferentPasswords_ProduceDifferentHashes()
        {
            // Arrange
            string password1 = "PasswordOne123!";
            string password2 = "PasswordTwo456!";
            
            // Act
            string hash1 = _passwordHasher.HashPassword(password1);
            string hash2 = _passwordHasher.HashPassword(password2);
            
            // Assert
            hash1.Should().NotBe(hash2, "不同的密码应该生成不同的哈希值");
        }
        
        [Fact]
        public void VerifyPassword_InvalidHashFormat_ReturnsFalse()
        {
            // Arrange
            string password = "TestPassword123!";
            string invalidHash = "this_is_not_a_valid_hash";
            
            // Act
            bool result = _passwordHasher.VerifyPassword(password, invalidHash);
            
            // Assert
            result.Should().BeFalse("无效的哈希格式应该返回false");
        }
    }
}