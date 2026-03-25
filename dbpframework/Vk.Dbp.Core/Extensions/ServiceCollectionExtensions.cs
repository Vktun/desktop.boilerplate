using Microsoft.Extensions.DependencyInjection;
using Vk.Dbp.Core.Audit.Interfaces;
using Vk.Dbp.Core.Audit.Services;

namespace Vk.Dbp.Core.Extensions
{
    /// <summary>
    /// 服务集合扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加审计日志服务
        /// </summary>
        public static IServiceCollection AddAuditLog(this IServiceCollection services)
        {
            services.AddSingleton<IAuditLogService, AuditLogService>();
            return services;
        }
    }
}