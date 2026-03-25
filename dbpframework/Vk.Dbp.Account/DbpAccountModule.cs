using Microsoft.Extensions.DependencyInjection;
using System;
using Vk.Dbp.Account.Users;
using Vk.Dbp.Core;

namespace Vk.Dbp.Account
{
    public class DbpAccountModule : IDbpModule
    {
        public void RegisterTypes(IServiceCollection serviceCollection)
        {
           serviceCollection.AddSingleton<ICurrentUser, CurrentUser>();
        }
    }
}
