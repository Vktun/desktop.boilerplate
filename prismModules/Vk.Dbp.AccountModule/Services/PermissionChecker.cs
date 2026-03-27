using System;
using System.Threading.Tasks;
using Vk.Dbp.AccountModule.Models;

namespace Vk.Dbp.AccountModule.Services
{
    public class PermissionChecker : IPermissionChecker
    {
        private readonly IPermissionService _permissionService;

        public PermissionChecker(IPermissionService permissionService)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        }

        public async Task<bool> IsGrantedAsync(string permissionCode)
        {
            var session = UserSession.Instance;

            if (!session.IsLoggedIn)
                return false;

            return await IsGrantedAsync(session.UserId, permissionCode);
        }

        public async Task<bool> IsGrantedAsync(int userId, string permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode))
                throw new ArgumentException("权限编码不能为空", nameof(permissionCode));

            return await _permissionService.HasPermissionAsync(userId, permissionCode);
        }

        public bool IsGranted(string permissionCode)
        {
            var session = UserSession.Instance;

            if (!session.IsLoggedIn)
                return false;

            return session.HasPermission(permissionCode);
        }
    }
}
