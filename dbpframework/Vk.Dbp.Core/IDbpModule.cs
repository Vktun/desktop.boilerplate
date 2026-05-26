using Microsoft.Extensions.DependencyInjection;
using System;

namespace Vk.Dbp.Core
{
    /// <summary>
    /// Defines a framework module that can register its services.
    /// </summary>
    public interface IDbpModule
    {
         /// <summary>
         /// Registers module services in the dependency injection container.
         /// </summary>
         /// <param name="serviceCollection">The service collection to register into.</param>
         void RegisterTypes(IServiceCollection serviceCollection);
    }
}
