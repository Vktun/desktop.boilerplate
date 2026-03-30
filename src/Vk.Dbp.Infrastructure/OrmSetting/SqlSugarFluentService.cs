using Dabp.Infrastructure.Entities;
using SqlSugar;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

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
                        p.IsPrimarykey = true;
                        if (p.PropertyInfo.PropertyType == typeof(int) || p.PropertyInfo.PropertyType == typeof(long))
                        {
                            p.IsIdentity = true;
                        }
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
                    else
                    {
                        var propertyType = p.PropertyInfo.PropertyType;
                        var underlyingType = Nullable.GetUnderlyingType(propertyType);
                        if (underlyingType != null)
                        {
                            p.IsNullable = true;
                        }
                        else if (propertyType == typeof(string) || propertyType == typeof(byte[]))
                        {
                            p.IsNullable = true;
                        }
                        else
                        {
                            p.IsNullable = false;
                        }
                    }

                    p.IfTable<AuditLog>();

                    p.IfTable<User>();

                    p.IfTable<Role>();

                    p.IfTable<Permission>();

                    p.IfTable<OrganizationUnit>();

                    p.IfTable<UserRole>().UpdateProperty(t => t.UserId, it => { it.IsIdentity = false; })
                                        .UpdateProperty(t => t.RoleId, it => { it.IsIdentity = false; });

                    p.IfTable<RolePermission>().UpdateProperty(t => t.PermissionId, it => { it.IsIdentity = false; })
                                        .UpdateProperty(t => t.RoleId, it => { it.IsIdentity = false; });

                    p.IfTable<UserOrganizationUnit>().UpdateProperty(t => t.UserId, it => { it.IsIdentity = false; })
                                        .UpdateProperty(t => t.OrganizationUnitId, it => { it.IsIdentity = false; });

                    p.IfTable<RoleOrganizationUnit>().UpdateProperty(t => t.OrganizationUnitId, it => { it.IsIdentity = false; })
                                        .UpdateProperty(t => t.RoleId, it => { it.IsIdentity = false; });
                },
            };
        }
    }
}
