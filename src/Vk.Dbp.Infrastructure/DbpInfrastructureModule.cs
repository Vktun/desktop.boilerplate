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
            serviceCollection.AddScoped(typeof(IRepository<>), typeof(SqlSugarRepository<>));
        }
    }
}
