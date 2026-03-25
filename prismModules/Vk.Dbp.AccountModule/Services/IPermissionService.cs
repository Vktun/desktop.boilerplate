using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vk.Dbp.AccountModule.Models;

namespace Vk.Dbp.AccountModule.Services
{
    /// <summary>
    /// 权限服务接口
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// 获取所有权限
        /// </summary>
        Task<List<Permission>> GetAllPermissionsAsync();

        /// <summary>
        /// 获取权限树（按类型）
        /// </summary>
        Task<List<Permission>> GetPermissionTreeAsync(PermissionType? type = null);

        /// <summary>
        /// 按ID获取权限
        /// </summary>
        Task<Permission> GetPermissionByIdAsync(int id);

        /// <summary>
        /// 创建权限
        /// </summary>
        Task<bool> CreatePermissionAsync(Permission permission);

        /// <summary>
        /// 更新权限
        /// </summary>
        Task<bool> UpdatePermissionAsync(Permission permission);

        /// <summary>
        /// 删除权限
        /// </summary>
        Task<bool> DeletePermissionAsync(int id);

        /// <summary>
        /// 获取用户的权限
        /// </summary>
        Task<List<Permission>> GetUserPermissionsAsync(int userId);

        /// <summary>
        /// 检查用户是否拥有某个权限
        /// </summary>
        Task<bool> HasPermissionAsync(int userId, string permissionCode);

        /// <summary>
        /// 禁用/启用权限
        /// </summary>
        Task<bool> EnablePermissionAsync(int id, bool isEnabled);
    }
}