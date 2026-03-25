using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vk.Dbp.AccountModule.Models;

namespace Vk.Dbp.AccountModule.Services
{
    /// <summary>
    /// 角色服务接口
    /// </summary>
    public interface IRoleService
    {
        /// <summary>
        /// 获取所有角色
        /// </summary>
        Task<List<Role>> GetAllRolesAsync();

        /// <summary>
        /// 按ID获取角色
        /// </summary>
        Task<Role> GetRoleByIdAsync(int id);

        /// <summary>
        /// 创建角色
        /// </summary>
        Task<bool> CreateRoleAsync(Role role);

        /// <summary>
        /// 更新角色
        /// </summary>
        Task<bool> UpdateRoleAsync(Role role);

        /// <summary>
        /// 删除角色
        /// </summary>
        Task<bool> DeleteRoleAsync(int id);

        /// <summary>
        /// 为角色分配权限
        /// </summary>
        Task<bool> AssignPermissionsToRoleAsync(int roleId, List<int> permissionIds);

        /// <summary>
        /// 获取角色的权限
        /// </summary>
        Task<List<Permission>> GetRolePermissionsAsync(int roleId);

        /// <summary>
        /// 禁用/启用角色
        /// </summary>
        Task<bool> EnableRoleAsync(int id, bool isEnabled);
    }
}