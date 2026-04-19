using Vk.Dbp.Services.Session;

namespace Vk.Dbp.Services.Audit;

/// <summary>
/// 审计身份扩展方法 - 从用户会话获取审计所需的用户身份信息
/// </summary>
public static class AuditIdentityExtensions
{
    /// <summary>
    /// 获取审计用户ID
    /// </summary>
    /// <param name="userInfo">用户信息（可为空）</param>
    /// <returns>已登录返回用户ID，未登录返回0</returns>
    public static int GetAuditUserId(this IUserInfo? userInfo)
        => userInfo?.IsLoggedIn == true ? userInfo.UserId : 0;

    /// <summary>
    /// 获取审计用户名
    /// </summary>
    /// <param name="userInfo">用户信息（可为空）</param>
    /// <param name="fallback">未登录时的默认用户名</param>
    /// <returns>已登录返回用户名，未登录返回fallback（默认为"system"）</returns>
    public static string GetAuditUsername(this IUserInfo? userInfo, string fallback = "system")
    {
        if (userInfo?.IsLoggedIn != true)
        {
            return fallback;
        }

        return string.IsNullOrWhiteSpace(userInfo.Username)
            ? fallback
            : userInfo.Username;
    }
}