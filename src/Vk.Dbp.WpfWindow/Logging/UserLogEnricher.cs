using System;
using Serilog.Core;
using Serilog.Events;
using Vk.Dbp.AccountModule.Services;

namespace Dabp.WpfWindow.Logging
{
    /// <summary>
    /// 用户日志增强器 - 将当前用户信息添加到日志属性中
    /// </summary>
    public class UserLogEnricher : ILogEventEnricher
    {
        private readonly IUserSession _userSession;
        
        public UserLogEnricher(IUserSession userSession)
        {
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }
        
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // 添加用户ID
            if (_userSession.UserId > 0)
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("UserId", _userSession.UserId));
            }
            
            // 添加用户名
            if (!string.IsNullOrEmpty(_userSession.Username))
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("Username", _userSession.Username));
            }
            
            // 添加登录状态
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("IsLoggedIn", _userSession.IsLoggedIn));
        }
    }
}