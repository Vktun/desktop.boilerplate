using SqlSugar;
using Xunit;

namespace Vk.Dbp.Tests.Common
{
    public class TestDatabaseFixture : IDisposable
    {
        private readonly string _databasePath;

        public ISqlSugarClient Database { get; }

        public TestDatabaseFixture()
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"dbp-tests-{Guid.NewGuid():N}.db");

            Database = new SqlSugarScope(new ConnectionConfig
            {
                ConnectionString = $"Data Source={_databasePath}",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            Database.DbMaintenance.CreateDatabase();

            Database.CodeFirst.InitTables(
                typeof(Dabp.Infrastructure.Entities.User),
                typeof(Dabp.Infrastructure.Entities.Role),
                typeof(Dabp.Infrastructure.Entities.Permission),
                typeof(Dabp.Infrastructure.Entities.UserRole),
                typeof(Dabp.Infrastructure.Entities.UserOrganizationUnit),
                typeof(Dabp.Infrastructure.Entities.RolePermission),
                typeof(Dabp.Infrastructure.Entities.RoleOrganizationUnit),
                typeof(Dabp.Infrastructure.Entities.OrganizationUnit),
                typeof(Dabp.Infrastructure.Entities.AuditLog),
                typeof(Dabp.Infrastructure.Entities.Notification),
                typeof(Dabp.Infrastructure.Entities.SystemConfig),
                typeof(Dabp.Infrastructure.Entities.AlarmRecord),
                typeof(Dabp.Infrastructure.Entities.AlarmConfig));
        }

        public void Dispose()
        {
            Database?.Ado.Close();
            Database?.Dispose();

            for (var attempt = 0; attempt < 3 && File.Exists(_databasePath); attempt++)
            {
                try
                {
                    File.Delete(_databasePath);
                }
                catch (IOException) when (attempt < 2)
                {
                    DelayBeforeDeleteRetry(attempt);
                }
                catch (UnauthorizedAccessException) when (attempt < 2)
                {
                    DelayBeforeDeleteRetry(attempt);
                }
                catch (IOException)
                {
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    return;
                }
            }
        }

        private static void DelayBeforeDeleteRetry(int attempt)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(50 * (attempt + 1)));
        }
    }

    [CollectionDefinition("DatabaseCollection")]
    public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
    {
    }
}
