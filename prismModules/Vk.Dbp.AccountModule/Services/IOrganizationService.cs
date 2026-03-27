using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vk.Dbp.AccountModule.Models;

namespace Vk.Dbp.AccountModule.Services
{
    public interface IOrganizationService
    {
        Task<List<OrganizationUnitModel>> GetAllOrganizationUnitsAsync();

        Task<OrganizationUnitModel> GetOrganizationUnitByIdAsync(int id);

        Task<bool> CreateOrganizationUnitAsync(OrganizationUnitModel orgUnit);

        Task<bool> UpdateOrganizationUnitAsync(OrganizationUnitModel orgUnit);

        Task<bool> DeleteOrganizationUnitAsync(int id);

        Task<List<User>> GetOrganizationUsersAsync(int orgUnitId);

        Task<bool> AssignUsersToOrganizationAsync(int orgUnitId, List<int> userIds);

        Task<bool> RemoveUserFromOrganizationAsync(int orgUnitId, int userId);

        Task<List<OrganizationUnitModel>> GetUserOrganizationsAsync(int userId);

        Task<List<OrganizationUnitModel>> BuildOrganizationTreeAsync();
    }
}
