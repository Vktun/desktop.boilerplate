using SqlSugar;
using Xunit;

namespace Vk.Dbp.Tests.Common
{
    /// <summary>
    /// 测试数据库夹具 - 使用SQLite内存数据库
    /// </summary>
    public class TestDatabaseFixture : IDisposable
    {
        public ISqlSugarClient Database { get; }
        
        public TestDatabaseFixture()
        {
            // 使用SQLite内存数据库进行测试
            Database = new SqlSugarScope(new ConnectionConfig
            {
                ConnectionString = "DataSource=:memory:",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = false,
                InitKeyType = InitKeyType.Attribute
            });
            
            // 初始化数据库架构
            InitializeDatabase();
        }
        
        private void InitializeDatabase()
        {
            // 创建测试表结构
            Database.CodeFirst.InitTables(
                typeof(Dabp.Infrastructure.Entities.User),
                typeof(Dabp.Infrastructure.Entities.Role),
                typeof(Dabp.Infrastructure.Entities.Permission),
                typeof(Dabp.Infrastructure.Entities.UserRole),
                typeof(Dabp.Infrastructure.Entities.RolePermission),
                typeof(Dabp.Infrastructure.Entities.OrganizationUnit),
                typeof(Dabp.Infrastructure.Entities.AuditLog)
            );
        }
        
        public void Dispose()
        {
            Database?.Dispose();
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