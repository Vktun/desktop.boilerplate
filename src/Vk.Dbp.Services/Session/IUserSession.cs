using System;
using System.Collections.Generic;

namespace Vk.Dbp.Services.Session;

/// <summary>
/// 用户会话接口 - 保存当前登录用户的信息和状态
/// </summary>
public interface IUserSession : IUserInfo
{
    /// <summary>
    /// 真实姓名
    /// </summary>
    string RealName { get; }

    /// <summary>
    /// 邮箱
    /// </summary>
    string Email { get; }

    /// <summary>
    /// 电话
    /// </summary>
    string Phone { get; }

    /// <summary>
    /// 登录时间
    /// </summary>
    DateTime LoginTime { get; }

    /// <summary>
    /// 会话令牌
    /// </summary>
    string Token { get; }

    /// <summary>
    /// 用户权限列表
    /// </summary>
    List<string> Permissions { get; }

    /// <summary>
    /// 用户登录
    /// </summary>
    void Login(int userId, string username, string realName, string email, string phone, string token = "");

    /// <summary>
    /// 用户登出
    /// </summary>
    void Logout();

    /// <summary>
    /// 设置用户权限
    /// </summary>
    void SetPermissions(List<string> permissions);

    /// <summary>
    /// 检查用户是否拥有指定权限
    /// </summary>
    bool HasPermission(string permissionCode);

    /// <summary>
    /// 是否处于锁屏状态
    /// </summary>
    bool IsLocked { get; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    DateTime LastActivityTime { get; }

    /// <summary>
    /// 锁屏原因
    /// </summary>
    string LockReason { get; }

    /// <summary>
    /// 锁定会话
    /// </summary>
    void Lock(string reason);

    /// <summary>
    /// 解锁会话
    /// </summary>
    void Unlock();

    /// <summary>
    /// 更新活动时间
    /// </summary>
    void UpdateActivity();
}