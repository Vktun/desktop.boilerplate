using System;
using System.Threading.Tasks;

namespace Vk.Dbp.Contracts.Extensions
{
    /// <summary>
    /// 报表生成器接口 - 允许模块添加自定义报表
    /// </summary>
    public interface IReportGenerator
    {
        /// <summary>
        /// 报表类型标识
        /// </summary>
        string ReportType { get; }
        
        /// <summary>
        /// 报表显示名称
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// 报表描述
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// 生成报表
        /// </summary>
        /// <param name="parameters">报表参数</param>
        /// <returns>报表文件内容（如PDF、Excel等）</returns>
        Task<byte[]> GenerateReportAsync(ReportParameters parameters);
        
        /// <summary>
        /// 验证报表参数
        /// </summary>
        /// <param name="parameters">报表参数</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateParameters(ReportParameters parameters);
    }
    
    /// <summary>
    /// 报表参数
    /// </summary>
    public class ReportParameters
    {
        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate { get; set; }
        
        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate { get; set; }
        
        /// <summary>
        /// 组织单元ID（可选）
        /// </summary>
        public int? OrganizationId { get; set; }
        
        /// <summary>
        /// 用户ID（可选）
        /// </summary>
        public int? UserId { get; set; }
        
        /// <summary>
        /// 自定义参数（键值对）
        /// </summary>
        public Dictionary<string, object> CustomParameters { get; set; } = new();
    }
    
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// 错误消息列表
        /// </summary>
        public List<string> Errors { get; set; } = new();
        
        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static ValidationResult Success() => new() { IsValid = true };
        
        /// <summary>
        /// Creates a failed validation result with error messages.
        /// </summary>
        /// <param name="errors">The validation error messages.</param>
        public static ValidationResult Failure(params string[] errors) => new() 
        { 
            IsValid = false, 
            Errors = errors.ToList() 
        };
    }
}
