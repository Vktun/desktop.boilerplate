using System;
using System.Threading.Tasks;

namespace Dabp.Infrastructure
{
    public interface IDatabaseInitializer
    {
        Task InitializeAsync();
        void InitializeDatabase();
        Task InitializeDataAsync();
    }
}
