using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vk.Dbp.Contracts.Services
{
    /// <summary>
    /// 导出服务接口 - 提供数据导出功能
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// 导出数据到CSV文件
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">要导出的数据</param>
        /// <param name="fileName">文件名（不含扩展名）</param>
        /// <returns>导出文件的完整路径</returns>
        Task<string> ExportToCsvAsync<T>(IEnumerable<T> data, string fileName) where T : class;
        
        /// <summary>
        /// 导出数据到Excel文件
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">要导出的数据</param>
        /// <param name="fileName">文件名（不含扩展名）</param>
        /// <returns>导出文件的完整路径</returns>
        Task<string> ExportToExcelAsync<T>(IEnumerable<T> data, string fileName) where T : class;
        
        /// <summary>
        /// 从CSV文件导入数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="filePath">CSV文件路径</param>
        /// <returns>导入的数据列表</returns>
        Task<IEnumerable<T>> ImportFromCsvAsync<T>(string filePath) where T : class, new();
        
        /// <summary>
        /// 打开文件保存对话框并返回用户选择的路径
        /// </summary>
        /// <param name="defaultFileName">默认文件名</param>
        /// <param name="filter">文件过滤器（如 "CSV文件|*.csv"）</param>
        /// <returns>用户选择的文件路径，如果取消则返回null</returns>
        string? ShowSaveFileDialog(string defaultFileName, string filter);
        
        /// <summary>
        /// 打开文件选择对话框并返回用户选择的路径
        /// </summary>
        /// <param name="filter">文件过滤器</param>
        /// <returns>用户选择的文件路径，如果取消则返回null</returns>
        string? ShowOpenFileDialog(string filter);
    }
}