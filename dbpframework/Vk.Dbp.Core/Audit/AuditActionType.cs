namespace Vk.Dbp.Core.Audit
{
    /// <summary>
    /// 审计日志操作类型
    /// </summary>
    public enum AuditActionType
    {
        /// <summary>
        /// 创建
        /// </summary>
        Create = 1,

        /// <summary>
        /// 更新
        /// </summary>
        Update = 2,

        /// <summary>
        /// 删除
        /// </summary>
        Delete = 3,

        /// <summary>
        /// 登录
        /// </summary>
        Login = 4,

        /// <summary>
        /// 登出
        /// </summary>
        Logout = 5,

        /// <summary>
        /// 修改密码
        /// </summary>
        ChangePassword = 6,

        /// <summary>
        /// 导出
        /// </summary>
        Export = 7,

        /// <summary>
        /// 导入
        /// </summary>
        Import = 8,

        /// <summary>
        /// 打印
        /// </summary>
        Print = 9,

        /// <summary>
        /// 下载
        /// </summary>
        Download = 10,

        /// <summary>
        /// 查看详情
        /// </summary>
        View = 11
    }
}