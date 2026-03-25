using SqlSugar;
using System;
using Dabp.Infrastructure.Entities;
using System.Text;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Dabp.Infrastructure.OrmSetting
{
    public class SqlSugarFluentService
    {
        public SqlSugarFluentService() { }
        public static ConfigureExternalServices GetConfigureExternalServices()
        {
            return new ConfigureExternalServices()
            {
                EntityService = (s, p) =>
                {
                    var KeyAttribute = s.GetCustomAttribute<KeyAttribute>();
                    if (KeyAttribute != null)
                    {
                        p.IsPrimarykey=true;
                    }
                    var StringLengthAttribute = s.GetCustomAttribute<StringLengthAttribute>();
                    if (StringLengthAttribute != null)
                    {
                        p.Length = StringLengthAttribute.MaximumLength;
                    }
                    var AllowNullAttribute = s.GetCustomAttribute<AllowNullAttribute>();
                    if (AllowNullAttribute != null)
                    {
                        p.IsNullable = true;
                    }
                    
                    p.IfTable<AuditLog>()
                    .UpdateProperty(it => it.Parameters, it =>
                    {
                        it.IsNullable = true;
                        it.DataType = StaticConfig.CodeFirst_BigString;
                    })
                    .UpdateProperty(it => it.Exceptions, it =>
                     {
                         it.DataType = StaticConfig.CodeFirst_BigString;//支持多库的MaxString用法
                     });

                    p.IfTable<OrganizationUnit>();

                    p.IfTable<Permission>();

                    p.IfTable<Role>();

                    p.IfTable<RoleOrganizationUnit>();

                    p.IfTable<User>();

                    p.IfTable<UserOrganizationUnit>();

                    p.IfTable<UserRole>();

                },
            };
        }
    }
}