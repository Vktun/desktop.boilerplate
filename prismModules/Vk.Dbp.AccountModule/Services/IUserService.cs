using System.Collections.Generic;
using System.Threading.Tasks;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.Contracts.Data;

namespace Vk.Dbp.AccountModule.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();

        Task<PagedResult<User>> GetUsersPagedAsync(int pageIndex, int pageSize, string? keyword = null);

        Task<User?> GetUserByIdAsync(int id);

        Task<User?> GetUserByUsernameAsync(string username);

        Task<bool> CreateUserAsync(User user);

        Task<bool> UpdateUserAsync(User user);

        Task<bool> DeleteUserAsync(int id);

        Task<bool> EnableUserAsync(int id, bool isEnabled);

        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);

        Task<bool> ResetPasswordAsync(int userId, string newPassword);

        Task<bool> AssignRolesToUserAsync(int userId, List<int> roleIds);

        Task<List<Role>> GetUserRolesAsync(int userId);
    }
}
