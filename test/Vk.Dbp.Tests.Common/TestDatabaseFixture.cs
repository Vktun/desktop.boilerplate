using SqlSugar;
using Xunit;

namespace Vk.Dbp.Tests.Common
{
    /// <summary>
    /// 测试数据库夹具 - 使用SQLite内存数据库
    /// </summary>
    public class TestDatabaseFixture : IDisposable
    {
        private readonly string _databasePath;

        public ISqlSugarClient Database { get; }
        
        public TestDatabaseFixture()
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"dbp-tests-{Guid.NewGuid():N}.db");

            // 使用临时SQLite数据库进行测试，避免内存库连接生命周期导致 schema 丢失。
            Database = new SqlSugarScope(new ConnectionConfig
            {
                ConnectionString = $"Data Source={_databasePath}",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            
            // 初始化数据库架构
            InitializeDatabase();
        }
        
        private void InitializeDatabase()
        {
            Database.DbMaintenance.CreateDatabase();

            // 创建测试表结构
            Database.CodeFirst.InitTables(
                typeof(Dabp.Infrastructure.Entities.User),
                typeof(Dabp.Infrastructure.Entities.Role),
                typeof(Dabp.Infrastructure.Entities.Permission),
                typeof(Dabp.Infrastructure.Entities.UserRole),
                typeof(Dabp.Infrastructure.Entities.UserOrganizationUnit),
                typeof(Dabp.Infrastructure.Entities.RolePermission),
                typeof(Dabp.Infrastructure.Entities.OrganizationUnit),
                typeof(Dabp.Infrastructure.Entities.AuditLog)
            );
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
                catch (IOException)
                {
                    if (attempt == 2)
                    {
                        return;
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(50);
                }
            }
        }
    }
    
    /// <summary>
    /// 测试数据集合 - 共享数据库实例
    /// </summary>
    [CollectionDefinition("DatabaseCollection")]
    public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
    {
        // 这个类不需要任何实现，仅作为标记
    }
}
