using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Dabp.Utils.Exceptions;
using Vk.Dbp.Contracts.Services;

namespace Dabp.Services.Export
{
    /// <summary>
    /// 导出服务实现 - 提供CSV、Excel和PDF导出功能
    /// </summary>
    public class ExportService : IExportService
    {
        private static readonly HashSet<string> SafeExportExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".csv",
            ".xlsx",
            ".pdf"
        };

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
            
            filePath = ValidateExportPath(filePath, ".csv");

            var csv = new System.Text.StringBuilder();
            
            // 获取属性信息
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            // 写入标题行
            var headers = new List<string>(properties.Length);
            foreach (var property in properties)
            {
                headers.Add(property.Name);
            }

            csv.AppendLine(string.Join(",", headers));
            
            // 写入数据行
            foreach (var item in data)
            {
                var values = new List<string>(properties.Length);
                foreach (var property in properties)
                {
                    var value = property.GetValue(item);
                    values.Add(FormatCsvValue(value));
                }

                csv.AppendLine(string.Join(",", values));
            }
            
            await File.WriteAllTextAsync(filePath, csv.ToString(), System.Text.Encoding.UTF8);
            return filePath;
        }
        
        /// <summary>
        /// 导出数据到Excel文件（简化版本）
        /// </summary>
        public Task<string> ExportToExcelAsync<T>(IEnumerable<T> data, string fileName) where T : class
        {
            return ExportToExcelAsync<T>(data, fileName, null);
        }
        
        /// <summary>
        /// 导出数据到Excel文件（带配置选项）
        /// </summary>
        public async Task<string> ExportToExcelAsync<T>(IEnumerable<T> data, string fileName, ExcelExportOptions? options) where T : class
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            var filePath = ShowSaveFileDialog(fileName, "Excel文件|*.xlsx");
            if (string.IsNullOrEmpty(filePath))
                throw new OperationCanceledException("用户取消了保存操作");
            
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(options?.Title ?? "数据");
            
            // 获取属性信息（排除指定列）
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => options?.ExcludedColumns == null || !options.ExcludedColumns.Contains(p.Name))
                .ToList();
            
            // 写入标题行
            for (int col = 1; col <= properties.Count; col++)
            {
                var property = properties[col - 1];
                var displayName = GetColumnName(property, options);
                var cell = worksheet.Cell(1, col);
                cell.Value = displayName;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            
            // 写入数据行
            filePath = ValidateExportPath(filePath, ".xlsx");

            var dataList = data.ToList();
            for (int row = 2; row <= dataList.Count + 1; row++)
            {
                var item = dataList[row - 2];
                for (int col = 1; col <= properties.Count; col++)
                {
                    var property = properties[col - 1];
                    var value = property.GetValue(item);
                    var cell = worksheet.Cell(row, col);
                    
                    SetCellValue(cell, value, property, options);
                }
            }
            
            // 应用样式和功能
            if (dataList.Count > 0)
            {
                var range = worksheet.Range(1, 1, dataList.Count + 1, properties.Count);
                
                // 自动筛选
                if (options?.AutoFilter ?? true)
                {
                    range.SetAutoFilter();
                }
                
                // 冻结标题行
                if (options?.FreezeHeader ?? true)
                {
                    worksheet.SheetView.FreezeRows(1);
                }
            }
            
            // 调整列宽
            worksheet.Columns().AdjustToContents();
            
            // 保存文件
            workbook.SaveAs(filePath);
            
            return await Task.FromResult(filePath);
        }
        
        /// <summary>
        /// 导出数据到PDF文件
        /// </summary>
        public async Task<string> ExportToPdfAsync<T>(IEnumerable<T> data, string fileName, string? title = null) where T : class
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            var filePath = ShowSaveFileDialog(fileName, "PDF文件|*.pdf");
            if (string.IsNullOrEmpty(filePath))
                throw new OperationCanceledException("用户取消了保存操作");
            
            filePath = ValidateExportPath(filePath, ".pdf");

            var dataList = data.ToList();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            // 配置QuestPDF中文支持
            QuestPDF.Settings.License = LicenseType.Community;
            
            Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(20);
                        
                        page.Header()
                            .Text(title ?? "数据报表")
                            .FontSize(16)
                            .Bold()
                            .AlignCenter();
                        
                        page.Content()
                            .PaddingVertical(10)
                            .Table(table =>
                            {
                                // 定义列
                                table.ColumnsDefinition(columns =>
                                {
                                    for (int i = 0; i < properties.Length; i++)
                                    {
                                        columns.RelativeColumn();
                                    }
                                });
                                
                                // 表头
                                table.Header(header =>
                                {
                                    foreach (var property in properties)
                                    {
                                        header.Cell()
                                            .Background(Colors.Grey.Lighten3)
                                            .Padding(5)
                                            .Text(property.Name)
                                            .Bold()
                                            .AlignCenter();
                                    }
                                });
                                
                                // 数据行
                                foreach (var item in dataList)
                                {
                                    foreach (var property in properties)
                                    {
                                        var value = property.GetValue(item);
                                        var displayValue = FormatValue(value);
                                        
                                        table.Cell()
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(displayValue);
                                    }
                                }
                            });
                        
                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("页码: ");
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
                            });
                    });
                })
                .GeneratePdf(filePath);
            
            return await Task.FromResult(filePath);
        }
        
        /// <summary>
        /// 从CSV文件导入数据
        /// </summary>
        public async Task<IEnumerable<T>> ImportFromCsvAsync<T>(string filePath) where T : class, new()
        {
            filePath = ValidateReadableFilePath(filePath, ".csv");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);
            
            var lines = await File.ReadAllLinesAsync(filePath, System.Text.Encoding.UTF8);
            if (lines.Length < 2)
                return Enumerable.Empty<T>();
            
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var headerLine = lines[0];
            var headers = ParseCsvLine(headerLine);
            
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
        public virtual string? ShowSaveFileDialog(string defaultFileName, string filter)
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
        public virtual string? ShowOpenFileDialog(string filter)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = filter,
                RestoreDirectory = true
            };
            
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
        
        /// <summary>
        /// 打开导出的文件
        /// </summary>
        public async Task<bool> OpenExportedFileAsync(string filePath)
        {
            if (!TryValidateExportedFilePath(filePath, out var safeFilePath) || !File.Exists(safeFilePath))
                return false;
            
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = safeFilePath,
                    UseShellExecute = true
                });
                return await Task.FromResult(true);
            }
            catch (Exception ex) when (
                ExpectedOperationExceptionFilter.IsExpectedFileOperationException(ex) ||
                ex is InvalidOperationException)
            {
                return false;
            }
        }
        
        #region 私有辅助方法
        
        private static string ValidateExportPath(string filePath, string expectedExtension)
        {
            var fullPath = ValidateFilePath(filePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Export directory is required.", nameof(filePath));
            }

            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(directory);
            }

            if (!Path.GetExtension(fullPath).Equals(expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Export file must use the {expectedExtension} extension.", nameof(filePath));
            }

            return fullPath;
        }

        private static string ValidateReadableFilePath(string filePath, string expectedExtension)
        {
            var fullPath = ValidateFilePath(filePath);
            if (!Path.GetExtension(fullPath).Equals(expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Import file must use the {expectedExtension} extension.", nameof(filePath));
            }

            return fullPath;
        }

        private static bool TryValidateExportedFilePath(string filePath, out string safeFilePath)
        {
            safeFilePath = string.Empty;
            try
            {
                var fullPath = ValidateFilePath(filePath);
                if (!SafeExportExtensions.Contains(Path.GetExtension(fullPath)))
                {
                    return false;
                }

                safeFilePath = fullPath;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        private static string ValidateFilePath(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

            var fullPath = Path.GetFullPath(filePath);
            var fileName = Path.GetFileName(fullPath);
            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                fullPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                throw new ArgumentException("Invalid file path.", nameof(filePath));
            }

            return fullPath;
        }

        private string GetColumnName(PropertyInfo property, ExcelExportOptions? options)
        {
            // 优先使用配置的显示名称
            if (options?.ColumnDisplayNames != null && options.ColumnDisplayNames.TryGetValue(property.Name, out var displayName))
            {
                return displayName;
            }
            
            // 检查Display特性
            var displayAttribute = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
            if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.Name))
            {
                return displayAttribute.Name;
            }
            
            // 默认使用属性名
            return property.Name;
        }
        
        private void SetCellValue(IXLCell cell, object? value, PropertyInfo property, ExcelExportOptions? options)
        {
            if (value == null)
            {
                cell.Value = "";
                return;
            }
            
            // 处理枚举值
            if (value is Enum enumValue)
            {
                var displayValue = GetEnumDisplayValue(enumValue, options);
                cell.Value = displayValue;
                return;
            }
            
            // 处理DateTime
            if (value is DateTime dateTime)
            {
                cell.Value = dateTime;
                var format = options?.ColumnFormats?.GetValueOrDefault(property.Name) ?? "yyyy-MM-dd HH:mm:ss";
                cell.Style.DateFormat.Format = format;
                return;
            }
            
            // 处理数值类型
            if (value is decimal || value is double || value is float || value is int || value is long)
            {
                cell.Value = Convert.ToDouble(value);
                if (options?.ColumnFormats != null && options.ColumnFormats.TryGetValue(property.Name, out var format))
                {
                    cell.Style.NumberFormat.Format = format;
                }
                return;
            }
            
            // 处理布尔值
            if (value is bool boolValue)
            {
                cell.Value = boolValue ? "是" : "否";
                return;
            }
            
            // 其他类型直接转为字符串
            cell.Value = value.ToString() ?? "";
        }
        
        private string GetEnumDisplayValue(Enum enumValue, ExcelExportOptions? options)
        {
            var enumType = enumValue.GetType().FullName;
            
            // 检查配置的枚举映射
            if (options?.EnumMappings != null && enumType != null)
            {
                if (options.EnumMappings.TryGetValue(enumType, out var mapping))
                {
                    // 枚举值需要封箱才能作为字典key
                    var boxedValue = (object)enumValue;
                    if (mapping.TryGetValue(boxedValue, out var displayText))
                    {
                        return displayText;
                    }
                }
            }
            
            // 使用Display特性
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
            var displayAttribute = fieldInfo?.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
            if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.Name))
            {
                return displayAttribute.Name;
            }
            
            // 默认返回枚举名称
            return enumValue.ToString();
        }
        
        private string FormatValue(object? value)
        {
            if (value == null)
                return "";
            
            if (value is DateTime dateTime)
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            
            if (value is bool boolValue)
                return boolValue ? "是" : "否";
            
            return value.ToString() ?? "";
        }
        
        private string FormatCsvValue(object? value)
        {
            if (value == null)
                return "";
            
            var stringValue = EscapeCsvFormula(value.ToString() ?? "");
            
            // 如果包含逗号、引号或换行符，需要用引号包围并转义内部引号
            if (stringValue.Contains(",") || stringValue.Contains("\"") || stringValue.Contains("\n"))
            {
                return $"\"{stringValue.Replace("\"", "\"\"")}\"";
            }
            
            return stringValue;
        }

        private static string EscapeCsvFormula(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var trimmedValue = value.TrimStart();
            if (trimmedValue.Length == 0)
            {
                return value;
            }

            return trimmedValue[0] is '=' or '+' or '-' or '@' ? $"'{value}" : value;
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
        
        #endregion
    }
}
