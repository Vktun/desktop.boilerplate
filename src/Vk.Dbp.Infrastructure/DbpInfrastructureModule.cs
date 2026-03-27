using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using Dabp.Infrastructure.OrmSetting;
using Dabp.Infrastructure.Repositories;
using Vk.Dbp.Core;

namespace Dabp.Infrastructure
{
    public class DbpInfrastructureModule : IDbpModule
    {
        public void RegisterTypes(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ISqlSugarClient>(sp =>
            {
                var db = new SqlSugarScope(new ConnectionConfig()
                {
                    ConnectionString = "Data Source=local.db",
                    DbType = DbType.Sqlite,
                    IsAutoCloseConnection = true,
                    ConfigureExternalServices = SqlSugarFluentService.GetConfigureExternalServices()
                });
                return db;
            });

            serviceCollection.AddScoped(typeof(IRepository<>), typeof(SqlSugarRepository<>));
            serviceCollection.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        }
    }
}
