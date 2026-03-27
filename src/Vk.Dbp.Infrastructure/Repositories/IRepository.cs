using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Dabp.Infrastructure.Repositories
{
    public interface IRepository<T> where T : class, new()
    {
        Task<T> GetByIdAsync(object id);
        Task<T> GetFirstAsync(Expression<Func<T, bool>> whereExpression);
        Task<List<T>> GetListAsync();
        Task<List<T>> GetListAsync(Expression<Func<T, bool>> whereExpression);
        Task<int> InsertAsync(T entity);
        Task<int> InsertRangeAsync(List<T> entities);
        Task<int> UpdateAsync(T entity);
        Task<int> UpdateAsync(Expression<Func<T, bool>> columns, Expression<Func<T, bool>> whereExpression);
        Task<int> UpdateRangeAsync(List<T> entities);
        Task<int> DeleteAsync(T entity);
        Task<int> DeleteAsync(Expression<Func<T, bool>> whereExpression);
        Task<int> DeleteByIdAsync(object id);
        Task<int> DeleteRangeAsync(List<T> entities);
        Task<bool> IsAnyAsync(Expression<Func<T, bool>> whereExpression);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> whereExpression);
        Task<(List<T> list, int total)> GetPageListAsync(Expression<Func<T, bool>> whereExpression, int pageIndex, int pageSize, Expression<Func<T, object>> orderByExpression = null, bool isAsc = true);
    }
}
