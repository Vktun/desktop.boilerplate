using System;
using System.Threading.Tasks;
using Vk.Dbp.AccountModule.Models;

namespace Vk.Dbp.AccountModule.Services
{
    public class PermissionChecker : IPermissionChecker
    {
        private readonly IPermissionService _permissionService;
        private readonly IUserSession _userSession;

        public PermissionChecker(IPermissionService permissionService, IUserSession userSession)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }

        public async Task<bool> IsGrantedAsync(string permissionCode)
        {
            if (!_userSession.IsLoggedIn)
                return false;

            return await IsGrantedAsync(_userSession.UserId, permissionCode);
        }

        public async Task<bool> IsGrantedAsync(int userId, string permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode))
                throw new ArgumentException("权限编码不能为空", nameof(permissionCode));

            return await _permissionService.HasPermissionAsync(userId, permissionCode);
        }

        public bool IsGranted(string permissionCode)
        {
            if (!_userSession.IsLoggedIn)
                return false;

            return _userSession.HasPermission(permissionCode);
        }
    }
}
