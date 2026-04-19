namespace Vk.Dbp.Services.Session;

/// <summary>
/// 轻量用户信息接口 - 用于审计日志获取用户身份
/// </summary>
public interface IUserInfo
{
    /// <summary>
    /// 用户ID
    /// </summary>
    int UserId { get; }

    /// <summary>
    /// 用户名
    /// </summary>
    string Username { get; }

    /// <summary>
    /// 是否已登录
    /// </summary>
    bool IsLoggedIn { get; }
}