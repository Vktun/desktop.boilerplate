using Microsoft.Extensions.DependencyInjection;
using System;

namespace Vk.Dbp.Core
{
    public interface IDbpModule
    {
         void RegisterTypes(IServiceCollection serviceCollection);
    }
}
