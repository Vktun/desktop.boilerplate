using System;
using System.Threading.Tasks;
using Dabp.Utils.Security;
using Vk.Dbp.Contracts.Services;

namespace Vk.Dbp.AccountModule.Services
{
    /// <summary>
    /// 用户凭据校验实现 - 供锁屏解锁等 shell 场景使用，密码哈希不出账户域
    /// </summary>
    public class UserCredentialService : IUserCredentialService
    {
        private readonly IUserService _userService;
        private readonly IPasswordHasher _passwordHasher;

        public UserCredentialService(IUserService userService, IPasswordHasher passwordHasher)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        public async Task<PasswordVerificationResult> VerifyUserPasswordAsync(int userId, string password)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return PasswordVerificationResult.UserNotFound;
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return PasswordVerificationResult.NoPasswordHash;
            }

            return _passwordHasher.VerifyPassword(password, user.PasswordHash)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Mismatch;
        }
    }
}
