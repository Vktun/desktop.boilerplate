using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Mvvm;

namespace Vk.Dbp.Services.Session;

/// <summary>
/// 用户会话信息 - 保存当前登录用户的信息和状态
/// </summary>
public class UserSession : BindableBase, IUserSession
{
    private const string AdminUsername = "admin";

    private int _userId;
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId
    {
        get => _userId;
        set => SetProperty(ref _userId, value);
    }

    private string? _username;
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username
    {
        get => _username ?? string.Empty;
        set => SetProperty(ref _username, value);
    }

    private string? _realName;
    /// <summary>
    /// 真实姓名
    /// </summary>
    public string RealName
    {
        get => _realName ?? string.Empty;
        set => SetProperty(ref _realName, value);
    }

    private string? _email;
    /// <summary>
    /// 邮箱
    /// </summary>
    public string Email
    {
        get => _email ?? string.Empty;
        set => SetProperty(ref _email, value);
    }

    private string? _phone;
    /// <summary>
    /// 电话
    /// </summary>
    public string Phone
    {
        get => _phone ?? string.Empty;
        set => SetProperty(ref _phone, value);
    }

    private bool _isLoggedIn;
    /// <summary>
    /// 是否已登录
    /// </summary>
    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set => SetProperty(ref _isLoggedIn, value);
    }

    private DateTime _loginTime;
    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTime LoginTime
    {
        get => _loginTime;
        set => SetProperty(ref _loginTime, value);
    }

    private string? _token;
    /// <summary>
    /// 会话令牌
    /// </summary>
    public string Token
    {
        get => _token ?? string.Empty;
        set => SetProperty(ref _token, value);
    }

    private List<string> _permissions = new();
    /// <summary>
    /// 用户权限列表
    /// </summary>
    public List<string> Permissions
    {
        get => _permissions;
        private set => SetProperty(ref _permissions, value);
    }

    private bool _isLocked;
    /// <summary>
    /// 是否处于锁屏状态
    /// </summary>
    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }

    private DateTime _lastActivityTime = DateTime.Now;
    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivityTime
    {
        get => _lastActivityTime;
        set => SetProperty(ref _lastActivityTime, value);
    }

    private string? _lockReason;
    /// <summary>
    /// 锁屏原因
    /// </summary>
    public string LockReason
    {
        get => _lockReason ?? string.Empty;
        set => SetProperty(ref _lockReason, value);
    }

    /// <summary>
    /// 公共构造函数，支持依赖注入
    /// </summary>
    public UserSession()
    {
        IsLoggedIn = false;
        Permissions = new List<string>();
        LastActivityTime = DateTime.Now;
    }

    /// <summary>
    /// 设置用户信息并标记为已登录
    /// </summary>
    public void Login(int userId, string username, string realName, string email, string phone, string token = "")
    {
        UserId = userId;
        Username = username;
        RealName = realName;
        Email = email;
        Phone = phone;
        Token = token;
        LoginTime = DateTime.Now;
        IsLoggedIn = true;
    }

    /// <summary>
    /// 注销用户并清除所有信息
    /// </summary>
    public void Logout()
    {
        UserId = 0;
        Username = string.Empty;
        RealName = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        Token = string.Empty;
        IsLoggedIn = false;
        LoginTime = default;
        Permissions = new List<string>();
    }

    /// <summary>
    /// 设置用户权限
    /// </summary>
    public void SetPermissions(List<string> permissions)
    {
        Permissions = permissions ?? new List<string>();
    }

    /// <summary>
    /// 检查用户是否拥有指定权限
    /// </summary>
    public bool HasPermission(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            return false;

        if (string.Equals(Username, AdminUsername, StringComparison.OrdinalIgnoreCase))
            return true;

        return Permissions?.Any(permission =>
            string.Equals(permission, permissionCode, StringComparison.OrdinalIgnoreCase)) ?? false;
    }

    /// <summary>
    /// 锁定会话 - 保留用户信息，仅设置锁屏状态
    /// </summary>
    public void Lock(string reason)
    {
        LockReason = reason;
        IsLocked = true;
    }

    /// <summary>
    /// 解锁会话 - 清除锁屏状态，更新活动时间
    /// </summary>
    public void Unlock()
    {
        IsLocked = false;
        LockReason = string.Empty;
        UpdateActivity();
    }

    /// <summary>
    /// 更新活动时间
    /// </summary>
    public void UpdateActivity()
    {
        LastActivityTime = DateTime.Now;
    }
}
