using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Dabp.Infrastructure.Repositories
{
    public class SqlSugarRepository<T> : IRepository<T> where T : class, new()
    {
        private readonly ISqlSugarClient _db;

        public SqlSugarRepository(ISqlSugarClient db)
        {
            _db = db;
        }

        public async Task<T> GetByIdAsync(object id)
        {
            return await _db.Queryable<T>().InSingleAsync(id);
        }

        public async Task<T> GetFirstAsync(Expression<Func<T, bool>> whereExpression)
        {
            return await _db.Queryable<T>().FirstAsync(whereExpression);
        }

        public async Task<List<T>> GetListAsync()
        {
            return await _db.Queryable<T>().ToListAsync();
        }

        public async Task<List<T>> GetListAsync(Expression<Func<T, bool>> whereExpression)
        {
            return await _db.Queryable<T>().Where(whereExpression).ToListAsync();
        }

        public async Task<int> InsertAsync(T entity)
        {
            return await _db.Insertable(entity).ExecuteCommandAsync();
        }

        public async Task<int> InsertRangeAsync(List<T> entities)
        {
            return await _db.Insertable(entities).ExecuteCommandAsync();
        }

        public async Task<int> UpdateAsync(T entity)
        {
            return await _db.Updateable(entity).ExecuteCommandAsync();
        }

        public async Task<int> UpdateAsync(Expression<Func<T, bool>> columns, Expression<Func<T, bool>> whereExpression)
        {
            return await _db.Updateable<T>().SetColumns(columns).Where(whereExpression).ExecuteCommandAsync();
        }

        public async Task<int> UpdateRangeAsync(List<T> entities)
        {
            return await _db.Updateable(entities).ExecuteCommandAsync();
        }

        public async Task<int> DeleteAsync(T entity)
        {
            return await _db.Deleteable(entity).ExecuteCommandAsync();
        }

        public async Task<int> DeleteAsync(Expression<Func<T, bool>> whereExpression)
        {
            return await _db.Deleteable<T>().Where(whereExpression).ExecuteCommandAsync();
        }

        public async Task<int> DeleteByIdAsync(object id)
        {
            return await _db.Deleteable<T>().In(id).ExecuteCommandAsync();
        }

        public async Task<int> DeleteRangeAsync(List<T> entities)
        {
            return await _db.Deleteable(entities).ExecuteCommandAsync();
        }

        public async Task<bool> IsAnyAsync(Expression<Func<T, bool>> whereExpression)
        {
            return await _db.Queryable<T>().AnyAsync(whereExpression);
        }

        public async Task<int> CountAsync()
        {
            return await _db.Queryable<T>().CountAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> whereExpression)
        {
            return await _db.Queryable<T>().Where(whereExpression).CountAsync();
        }

        public async Task<(List<T> list, int total)> GetPageListAsync(Expression<Func<T, bool>> whereExpression, int pageIndex, int pageSize, Expression<Func<T, object>> orderByExpression = null, bool isAsc = true)
        {
            RefAsync<int> total = 0;
            var query = _db.Queryable<T>().Where(whereExpression);
            
            if (orderByExpression != null)
            {
                query = isAsc ? query.OrderBy(orderByExpression, OrderByType.Asc) : query.OrderBy(orderByExpression, OrderByType.Desc);
            }
            
            var list = await query.ToPageListAsync(pageIndex, pageSize, total);
            return (list, total);
        }
    }
}
