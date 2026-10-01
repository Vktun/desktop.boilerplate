using System.Threading.Tasks;

namespace Vk.Dbp.Contracts.Services
{
    /// <summary>密码校验结果</summary>
    public enum PasswordVerificationResult
    {
        /// <summary>密码正确</summary>
        Success,

        /// <summary>用户不存在</summary>
        UserNotFound,

        /// <summary>用户未设置密码哈希</summary>
        NoPasswordHash,

        /// <summary>密码不匹配</summary>
        Mismatch
    }

    /// <summary>
    /// 用户凭据校验契约（实现位于 AccountModule）。
    /// 锁屏解锁等 shell 场景只关心"密码是否正确"，不应触碰用户实体与密码哈希本身。
    /// </summary>
    public interface IUserCredentialService
    {
        /// <summary>校验指定用户的密码</summary>
        Task<PasswordVerificationResult> VerifyUserPasswordAsync(int userId, string password);
    }
}
