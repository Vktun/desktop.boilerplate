using System.Threading.Tasks;

namespace Vk.Dbp.AccountModule.Services
{
    public interface IPermissionChecker
    {
        Task<bool> IsGrantedAsync(string permissionCode);

        Task<bool> IsGrantedAsync(int userId, string permissionCode);

        bool IsGranted(string permissionCode);
    }
}
