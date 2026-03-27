using SqlSugar;
using System;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Dabp.Utils.Security;

namespace Dabp.Infrastructure
{
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly ISqlSugarClient _db;
        private readonly IPasswordHasher _passwordHasher;

        public DatabaseInitializer(ISqlSugarClient db, IPasswordHasher passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public async Task InitializeAsync()
        {
            await InitializeDatabaseAsync();
            await InitializeDataAsync();
        }

        public async Task InitializeDatabaseAsync()
        {
            if (!_db.DbMaintenance.GetDataBaseList().Contains(_db.Ado.Connection.Database))
            {
                _db.DbMaintenance.CreateDatabase();
            }

            _db.CodeFirst.InitTables(
                typeof(User),
                typeof(Role),
                typeof(Permission),
                typeof(OrganizationUnit),
                typeof(UserRole),
                typeof(UserOrganizationUnit),
                typeof(RoleOrganizationUnit),
                typeof(RolePermission),
                typeof(AuditLog)
            );

            await Task.CompletedTask;
        }

        public async Task InitializeDataAsync()
        {
            if (!await _db.Queryable<User>().AnyAsync())
            {
                var defaultUser = new User
                {
                    UserName = "admin",
                    PasswordHash = _passwordHasher.HashPassword("admin123"),
                    SurName = "系统管理员",
                    PhoneNumber = "13800138000",
                    IsActive = true,
                    ChangePasswordLastTime = DateTime.Now,
                    ValideDays = 90,
                    CreationTime = DateTime.Now,
                    CreatorId = 0,
                    IsDeleted = false
                };
                await _db.Insertable(defaultUser).ExecuteCommandAsync();
            }

            await Task.CompletedTask;
        }
    }
}
