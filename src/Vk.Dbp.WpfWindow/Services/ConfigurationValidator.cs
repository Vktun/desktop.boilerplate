using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Dabp.WpfWindow.Services
{
    /// <summary>
    /// 配置验证器 - 确保必要的配置项已正确设置
    /// </summary>
    public static class ConfigurationValidator
    {
        /// <summary>
        /// 验证配置
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <exception cref="InvalidOperationException">配置无效时抛出</exception>
        public static void Validate(IConfiguration configuration)
        {
            var errors = new List<string>();
            
            // 验证数据库连接字符串
            var connectionString = configuration.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                errors.Add("数据库连接字符串未配置。请设置 ConnectionStrings:Default 或环境变量 ConnectionStrings__Default");
            }
            
            // 验证安全配置（可选，有默认值）
            var securitySection = configuration.GetSection("Security");
            if (securitySection.Exists())
            {
                var tokenExpiryHours = configuration.GetValue<int?>("Security:TokenExpiryHours");
                if (tokenExpiryHours.HasValue && tokenExpiryHours.Value <= 0)
                {
                    errors.Add("Security:TokenExpiryHours 必须大于0");
                }
            }
            
            // 如果有错误，抛出异常
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "配置验证失败:\n" + string.Join("\n", errors));
            }
        }
        
        /// <summary>
        /// 打印配置信息（用于调试）
        /// </summary>
        public static void PrintConfiguration(IConfiguration configuration)
        {
            Console.WriteLine("=== 当前配置 ===");
            Console.WriteLine($"数据库连接: {MaskConnectionString(configuration.GetConnectionString("Default"))}");
            Console.WriteLine($"Redis配置: {configuration["Redis:Configuration"] ?? "未配置"}");
            Console.WriteLine($"环境: {configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production"}");
            Console.WriteLine("================");
        }
        
        /// <summary>
        /// 遮蔽敏感信息
        /// </summary>
        private static string? MaskConnectionString(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "未配置";
            
            // 遮蔽密码
            return System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                "(Password|Pwd|Password)=([^;]+)",
                "$1=****",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }
    }
}