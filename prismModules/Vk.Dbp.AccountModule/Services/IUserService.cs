using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vk.Dbp.AccountModule.Models;

namespace Vk.Dbp.AccountModule.Services
{
    /// <summary>
    /// 用户服务接口
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 获取所有用户
        /// </summary>
        Task<List<User>> GetAllUsersAsync();

        /// <summary>
        /// 按ID获取用户
        /// </summary>
        Task<User> GetUserByIdAsync(int id);

        /// <summary>
        /// 按用户名获取用户
        /// </summary>
        Task<User> GetUserByUsernameAsync(string username);

        /// <summary>
        /// 创建用户
        /// </summary>
        Task<bool> CreateUserAsync(User user);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task<bool> UpdateUserAsync(User user);

        /// <summary>
        /// 删除用户
        /// </summary>
        Task<bool> DeleteUserAsync(int id);

        /// <summary>
        /// 禁用/启用用户
        /// </summary>
        Task<bool> EnableUserAsync(int id, bool isEnabled);

        /// <summary>
        /// 修改密码
        /// </summary>
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);

        /// <summary>
        /// 重置密码
        /// </summary>
        Task<bool> ResetPasswordAsync(int userId, string newPassword);

        /// <summary>
        /// 为用户分配角色
        /// </summary>
        Task<bool> AssignRolesToUserAsync(int userId, List<int> roleIds);

        /// <summary>
        /// 获取用户的角色
        /// </summary>
        Task<List<Role>> GetUserRolesAsync(int userId);
    }
}