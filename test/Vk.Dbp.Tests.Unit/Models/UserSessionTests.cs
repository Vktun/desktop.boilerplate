using System;
using System.Collections.Generic;
using FluentAssertions;
using Vk.Dbp.AccountModule.Models;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Models
{
    public class UserSessionTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var userSession = new UserSession();
            
            // Assert
            userSession.IsLoggedIn.Should().BeFalse("新创建的会话应该是未登录状态");
            userSession.Permissions.Should().NotBeNull().And.BeEmpty("新会话应该有空权限列表");
            userSession.UserId.Should().Be(0);
            userSession.Username.Should().BeNull();
            userSession.RealName.Should().BeNull();
        }
        
        [Fact]
        public void Login_WithValidUser_SetsIsLoggedInToTrue()
        {
            // Arrange
            var userSession = new UserSession();
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                RealName = "测试用户",
                Email = "test@example.com",
                Phone = "13800138000"
            };
            string token = "test-token-123";
            
            // Act
            userSession.Login(user, token);
            
            // Assert
            userSession.IsLoggedIn.Should().BeTrue("登录后应该是已登录状态");
            userSession.UserId.Should().Be(1);
            userSession.Username.Should().Be("testuser");
            userSession.RealName.Should().Be("测试用户");
            userSession.Email.Should().Be("test@example.com");
            userSession.Phone.Should().Be("13800138000");
            userSession.Token.Should().Be(token);
            userSession.LoginTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }
        
        [Fact]
        public void Login_WithNullUser_ThrowsArgumentNullException()
        {
            // Arrange
            var userSession = new UserSession();
            
            // Act
            Action act = () => userSession.Login(null!, "token");
            
            // Assert
            act.Should().Throw<ArgumentNullException>("传入null用户应该抛出异常");
        }
        
        [Fact]
        public void Logout_ClearsAllProperties()
        {
            // Arrange
            var userSession = new UserSession();
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                RealName = "测试用户"
            };
            userSession.Login(user, "token");
            userSession.SetPermissions(new List<string> { "permission1", "permission2" });
            
            // Act
            userSession.Logout();
            
            // Assert
            userSession.IsLoggedIn.Should().BeFalse("登出后应该是未登录状态");
            userSession.UserId.Should().Be(0);
            userSession.Username.Should().BeNull();
            userSession.RealName.Should().BeNull();
            userSession.Email.Should().BeNull();
            userSession.Phone.Should().BeNull();
            userSession.Token.Should().BeNull();
            userSession.Permissions.Should().NotBeNull().And.BeEmpty();
        }
        
        [Fact]
        public void HasPermission_UserHasPermission_ReturnsTrue()
        {
            // Arrange
            var userSession = new UserSession();
            userSession.SetPermissions(new List<string> { "user:view", "user:edit", "role:view" });
            
            // Act
            bool result = userSession.HasPermission("user:edit");
            
            // Assert
            result.Should().BeTrue("用户拥有该权限应该返回true");
        }
        
        [Fact]
        public void HasPermission_UserLacksPermission_ReturnsFalse()
        {
            // Arrange
            var userSession = new UserSession();
            userSession.SetPermissions(new List<string> { "user:view", "user:edit" });
            
            // Act
            bool result = userSession.HasPermission("admin:delete");
            
            // Assert
            result.Should().BeFalse("用户没有该权限应该返回false");
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void HasPermission_WithNullOrWhiteSpacePermission_ReturnsFalse(string permissionCode)
        {
            // Arrange
            var userSession = new UserSession();
            userSession.SetPermissions(new List<string> { "user:view" });
            
            // Act
            bool result = userSession.HasPermission(permissionCode);
            
            // Assert
            result.Should().BeFalse("空的权限代码应该返回false");
        }
        
        [Fact]
        public void SetPermissions_ReplacesExistingPermissions()
        {
            // Arrange
            var userSession = new UserSession();
            userSession.SetPermissions(new List<string> { "old:permission1", "old:permission2" });
            
            // Act
            userSession.SetPermissions(new List<string> { "new:permission1", "new:permission2", "new:permission3" });
            
            // Assert
            userSession.Permissions.Should().HaveCount(3, "新的权限应该替换旧权限");
            userSession.Permissions.Should().Contain("new:permission1");
            userSession.Permissions.Should().Contain("new:permission2");
            userSession.Permissions.Should().Contain("new:permission3");
            userSession.Permissions.Should().NotContain("old:permission1");
        }
        
        [Fact]
        public void SetPermissions_WithNull_CreatesEmptyList()
        {
            // Arrange
            var userSession = new UserSession();
            userSession.SetPermissions(new List<string> { "user:view" });
            
            // Act
            userSession.SetPermissions(null!);
            
            // Assert
            userSession.Permissions.Should().NotBeNull().And.BeEmpty("null权限应该被替换为空列表");
        }
        
        [Fact]
        public void Clear_ResetsAllProperties()
        {
            // Arrange
            var userSession = new UserSession();
            var user = new User { Id = 1, Username = "testuser" };
            userSession.Login(user, "token");
            
            // Act
            userSession.Clear();
            
            // Assert
            userSession.IsLoggedIn.Should().BeFalse();
            userSession.UserId.Should().Be(0);
        }
    }
}