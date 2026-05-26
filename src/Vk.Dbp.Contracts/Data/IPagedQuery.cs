using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Vk.Dbp.Contracts.Data
{
    /// <summary>
    /// 分页查询接口
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface IPagedQuery<T>
    {
        /// <summary>
        /// 页码（从1开始）
        /// </summary>
        int PageIndex { get; set; }
        
        /// <summary>
        /// 每页大小
        /// </summary>
        int PageSize { get; set; }
        
        /// <summary>
        /// 排序字段
        /// </summary>
        string? SortField { get; set; }
        
        /// <summary>
        /// 排序方向（asc/desc）
        /// </summary>
        string? SortDirection { get; set; }
        
        /// <summary>
        /// 过滤条件表达式
        /// </summary>
        Expression<Func<T, bool>>? Filter { get; set; }
    }
    
    /// <summary>
    /// 分页结果
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// 数据项列表
        /// </summary>
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// 当前页码
        /// </summary>
        public int PageIndex { get; set; }
        
        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; }
        
        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        
        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPreviousPage => PageIndex > 1;
        
        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages;
        
        /// <summary>
        /// 创建空结果
        /// </summary>
        public static PagedResult<T> Empty() => new();
    }
    
    /// <summary>
    /// 默认分页查询实现
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class PagedQuery<T> : IPagedQuery<T>
    {
        /// <summary>
        /// Gets or sets the 1-based page index.
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Gets or sets the field used for sorting.
        /// </summary>
        public string? SortField { get; set; }

        /// <summary>
        /// Gets or sets the sort direction.
        /// </summary>
        public string? SortDirection { get; set; } = "asc";

        /// <summary>
        /// Gets or sets the filter expression.
        /// </summary>
        public Expression<Func<T, bool>>? Filter { get; set; }
    }
}
