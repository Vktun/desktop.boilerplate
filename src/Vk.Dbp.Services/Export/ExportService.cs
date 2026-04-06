using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Win32;
using Vk.Dbp.Contracts.Services;

namespace Dabp.Services.Export
{
    /// <summary>
    /// 导出服务实现 - 提供CSV和Excel导出功能
    /// </summary>
    public class ExportService : IExportService
    {
        /// <summary>
        /// 导出数据到CSV文件
        /// </summary>
        public async Task<string> ExportToCsvAsync<T>(IEnumerable<T> data, string fileName) where T : class
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            var filePath = ShowSaveFileDialog(fileName, "CSV文件|*.csv");
            if (string.IsNullOrEmpty(filePath))
                throw new OperationCanceledException("用户取消了保存操作");
            
            var csv = new System.Text.StringBuilder();
            
            // 获取属性信息
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            // 写入标题行
            var headers = properties.Select(p => p.Name);
            csv.AppendLine(string.Join(",", headers));
            
            // 写入数据行
            foreach (var item in data)
            {
                var values = properties.Select(p =>
                {
                    var value = p.GetValue(item);
                    return FormatCsvValue(value);
                });
                csv.AppendLine(string.Join(",", values));
            }
            
            await File.WriteAllTextAsync(filePath, csv.ToString(), System.Text.Encoding.UTF8);
            return filePath;
        }
        
        /// <summary>
        /// 导出数据到Excel文件（简化实现，使用CSV格式但扩展名改为.xlsx）
        /// </summary>
        public async Task<string> ExportToExcelAsync<T>(IEnumerable<T> data, string fileName) where T : class
        {
            // 注意: 真正的Excel导出需要使用ClosedXML或EPPlus等库
            // 这里提供简化实现，使用CSV格式
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            var filePath = ShowSaveFileDialog(fileName, "Excel文件|*.xlsx");
            if (string.IsNullOrEmpty(filePath))
                throw new OperationCanceledException("用户取消了保存操作");
            
            // 简化实现: 导出为CSV
            // 实际项目中应该使用ClosedXML库生成真正的Excel文件
            var csvPath = Path.ChangeExtension(filePath, ".csv");
            await ExportToCsvAsync(data, Path.GetFileNameWithoutExtension(csvPath));
            
            // 重命名为xlsx（注意这不是真正的Excel格式）
            File.Move(csvPath, filePath, true);
            
            return filePath;
        }
        
        /// <summary>
        /// 从CSV文件导入数据
        /// </summary>
        public async Task<IEnumerable<T>> ImportFromCsvAsync<T>(string filePath) where T : class, new()
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);
            
            var lines = await File.ReadAllLinesAsync(filePath, System.Text.Encoding.UTF8);
            if (lines.Length < 2)
                return Enumerable.Empty<T>();
            
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var headerLine = lines[0];
            var headers = headerLine.Split(',').Select(h => h.Trim()).ToList();
            
            var result = new List<T>();
            
            for (int i = 1; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i]);
                if (values.Count != headers.Count)
                    continue;
                
                var item = new T();
                for (int j = 0; j < headers.Count && j < properties.Length; j++)
                {
                    var property = properties.FirstOrDefault(p => p.Name.Equals(headers[j], StringComparison.OrdinalIgnoreCase));
                    if (property != null && property.CanWrite)
                    {
                        var value = Convert.ChangeType(values[j], property.PropertyType);
                        property.SetValue(item, value);
                    }
                }
                result.Add(item);
            }
            
            return result;
        }
        
        /// <summary>
        /// 打开文件保存对话框
        /// </summary>
        public string? ShowSaveFileDialog(string defaultFileName, string filter)
        {
            var dialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = filter,
                RestoreDirectory = true
            };
            
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
        
        /// <summary>
        /// 打开文件选择对话框
        /// </summary>
        public string? ShowOpenFileDialog(string filter)
        {
            var dialog = OpenFileDialog()
            {
                Filter = filter,
                RestoreDirectory = true
            };
            
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
        
        private string FormatCsvValue(object? value)
        {
            if (value == null)
                return "";
            
            var stringValue = value.ToString() ?? "";
            
            // 如果包含逗号、引号或换行符，需要用引号包围并转义内部引号
            if (stringValue.Contains(",") || stringValue.Contains("\"") || stringValue.Contains("\n"))
            {
                return $"\"{stringValue.Replace("\"", "\"\"")}\"";
            }
            
            return stringValue;
        }
        
        private List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new System.Text.StringBuilder();
            bool inQuotes = false;
            
            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString().Trim());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }
            
            values.Add(currentValue.ToString().Trim());
            return values;
        }
    }
}