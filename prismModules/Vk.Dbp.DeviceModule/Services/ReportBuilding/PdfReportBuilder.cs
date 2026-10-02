using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Vk.Dbp.DeviceModule.Services.ReportBuilding;

/// <summary>
/// 报表 PDF 构建器（QuestPDF）：标题 + 报表期间 + 表格 → pdf 字节。
/// 全部文本显式雅黑字体（同 ExportService 的中文字体修复——QuestPDF 默认 Lato 无 CJK 字形）。
/// </summary>
internal static class PdfReportBuilder
{
    private const string ChineseFontFamily = "Microsoft YaHei";

    public static byte[] Build<T>(string title, string periodText, IReadOnlyList<T> rows, IReadOnlyList<ReportColumn<T>> columns)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);

                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text(title)
                        .FontFamily(ChineseFontFamily).FontSize(16).Bold();
                    column.Item().AlignCenter().Text(periodText)
                        .FontFamily(ChineseFontFamily).FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(columnsDefinition =>
                    {
                        foreach (var _ in columns)
                        {
                            columnsDefinition.RelativeColumn();
                        }
                    });

                    table.Header(header =>
                    {
                        foreach (var column in columns)
                        {
                            header.Cell()
                                .Background(Colors.Grey.Lighten3)
                                .Padding(4)
                                .Text(column.Header)
                                .FontFamily(ChineseFontFamily)
                                .Bold()
                                .AlignCenter();
                        }
                    });

                    foreach (var row in rows)
                    {
                        foreach (var column in columns)
                        {
                            var text = column.GetValue(row)?.ToString() ?? string.Empty;
                            table.Cell()
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(4)
                                .Text(text)
                                .FontFamily(ChineseFontFamily);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("页码: ").FontFamily(ChineseFontFamily);
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();

        return bytes;
    }
}
