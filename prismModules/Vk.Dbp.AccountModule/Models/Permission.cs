using System;

namespace Vk.Dbp.AccountModule.Models
{
    /// <summary>
    /// 权限类型
    /// </summary>
    public enum PermissionType
    {
        /// <summary>
        /// 菜单权限
        /// </summary>
        Menu = 1,

        /// <summary>
        /// 按钮权限
        /// </summary>
        Button = 2,

        /// <summary>
        /// API权限
        /// </summary>
        Api = 3
    }

    /// <summary>
    /// 权限模型
    /// </summary>
    public class Permission
    {
        public int Id { get; set; }

        /// <summary>
        /// 权限名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 权限编码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 权限类型
        /// </summary>
        public PermissionType Type { get; set; }

        /// <summary>
        /// 权限描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 所属模块
        /// </summary>
        public string Module { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 父权限ID（用于菜单树状结构）
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 子权限列表
        /// </summary>
        public List<Permission> Children { get; set; } = new List<Permission>();
    }
}