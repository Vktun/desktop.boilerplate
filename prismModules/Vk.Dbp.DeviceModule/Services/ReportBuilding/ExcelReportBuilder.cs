using System.IO;
using System.Reflection;
using ClosedXML.Excel;

namespace Vk.Dbp.DeviceModule.Services.ReportBuilding;

/// <summary>
/// 报表列定义（表头 + 取值器；生成器与预览共用）
/// </summary>
/// <typeparam name="T">行类型</typeparam>
/// <param name="Header">中文表头</param>
/// <param name="GetValue">单元格取值</param>
internal sealed record ReportColumn<T>(string Header, Func<T, object?> GetValue);

/// <summary>
/// 报表 Excel 构建器（ClosedXML）：标题 + 报表期间 + 表头 + 数据行 → xlsx 字节。
/// </summary>
internal static class ExcelReportBuilder
{
    public static byte[] Build<T>(string title, string periodText, IReadOnlyList<T> rows, IReadOnlyList<ReportColumn<T>> columns)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("报表");

        sheet.Cell(1, 1).Value = title;
        sheet.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);

        sheet.Cell(2, 1).Value = periodText;
        sheet.Cell(2, 1).Style.Font.SetFontColor(XLColor.Gray);

        var headerRow = 4;
        for (var i = 0; i < columns.Count; i++)
        {
            var cell = sheet.Cell(headerRow, i + 1);
            cell.Value = columns[i].Header;
            cell.Style.Font.SetBold();
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2F7");
        }

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                var value = columns[c].GetValue(rows[r]);
                var cell = sheet.Cell(headerRow + 1 + r, c + 1);
                cell.Value = value switch
                {
                    null => string.Empty,
                    int intValue => intValue,
                    long longValue => longValue,
                    double doubleValue => doubleValue,
                    decimal decimalValue => decimalValue,
                    DateTime dateTime => dateTime,
                    bool boolValue => boolValue,
                    _ => value.ToString() ?? string.Empty
                };
            }
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(headerRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
