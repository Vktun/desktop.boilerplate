using System;
using System.Threading.Tasks;

namespace Dabp.Infrastructure
{
    public interface IDatabaseInitializer
    {
        Task InitializeAsync();
        Task InitializeDatabaseAsync();
        Task InitializeDataAsync();
    }
}
