using System;
using System.Collections.Generic;
using Prism.Mvvm;

namespace Vk.Dbp.AccountModule.Models
{
    /// <summary>
    /// 用户会话信息 - 保存当前登录用户的信息和状态
    /// </summary>
    public class UserSession : BindableBase
    {
        private static UserSession _instance;
        private static readonly object _lockObject = new object();

        public static UserSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new UserSession();
                        }
                    }
                }
                return _instance;
            }
        }

        private int _userId;
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId
        {
            get { return _userId; }
            set { SetProperty(ref _userId, value); }
        }

        private string _username;
        /// <summary>
        /// 用户名
        /// </summary>
        public string Username
        {
            get { return _username; }
            set { SetProperty(ref _username, value); }
        }

        private string _realName;
        /// <summary>
        /// 真实姓名
        /// </summary>
        public string RealName
        {
            get { return _realName; }
            set { SetProperty(ref _realName, value); }
        }

        private string _email;
        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email
        {
            get { return _email; }
            set { SetProperty(ref _email, value); }
        }

        private string _phone;
        /// <summary>
        /// 电话
        /// </summary>
        public string Phone
        {
            get { return _phone; }
            set { SetProperty(ref _phone, value); }
        }

        private bool _isLoggedIn;
        /// <summary>
        /// 是否已登录
        /// </summary>
        public bool IsLoggedIn
        {
            get { return _isLoggedIn; }
            set { SetProperty(ref _isLoggedIn, value); }
        }

        private DateTime _loginTime;
        /// <summary>
        /// 登录时间
        /// </summary>
        public DateTime LoginTime
        {
            get { return _loginTime; }
            set { SetProperty(ref _loginTime, value); }
        }

        private string _token;
        public string Token
        {
            get { return _token; }
            set { SetProperty(ref _token, value); }
        }

        private List<string> _permissions;
        public List<string> Permissions
        {
            get { return _permissions; }
            private set { SetProperty(ref _permissions, value); }
        }

        /// <summary>
        /// 私有构造函数，防止外部实例化
        /// </summary>
        private UserSession()
        {
            IsLoggedIn = false;
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
        /// 使用完整的User对象设置登录信息
        /// </summary>
        public void Login(User user, string token = "")
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            Login(user.Id, user.Username, user.RealName, user.Email, user.Phone, token);
        }

        /// <summary>
        /// 注销用户并清除所有信息
        /// </summary>
        public void Logout()
        {
            UserId = 0;
            Username = null;
            RealName = null;
            Email = null;
            Phone = null;
            Token = null;
            IsLoggedIn = false;
            LoginTime = default;
            Permissions = null;
        }

        public void SetPermissions(List<string> permissions)
        {
            Permissions = permissions ?? new List<string>();
        }

        public bool HasPermission(string permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode))
                return false;

            return Permissions?.Contains(permissionCode) ?? false;
        }

        /// <summary>
        /// 清空所有用户信息
        /// </summary>
        public void Clear()
        {
            Logout();
        }

        /// <summary>
        /// 重置单例实例（用于测试或特殊场景）
        /// </summary>
        public static void ResetInstance()
        {
            lock (_lockObject)
            {
                _instance = null;
            }
        }
    }
}
