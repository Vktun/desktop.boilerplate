using Dabp.Services.Export;
using FluentAssertions;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class ExportServiceTests
{
    [Fact]
    public async Task ExportToCsvAsync_EscapesFormulaLikeValues()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"dbp-export-{Guid.NewGuid():N}.csv");
        var service = new TestExportService(filePath);

        try
        {
            var result = await service.ExportToCsvAsync(
                new[] { new CsvFormulaRow("=cmd|' /C calc'!A0") },
                "formula");

            result.Should().Be(filePath);
            var csv = await File.ReadAllTextAsync(filePath);
            csv.Should().Contain("'=cmd|' /C calc'!A0");
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task ImportFromCsvAsync_RejectsNonCsvPath()
    {
        var service = new TestExportService(null);

        Func<Task> act = async () => await service.ImportFromCsvAsync<CsvImportRow>("payload.txt");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*csv*");
    }

    [Fact]
    public async Task ExportToExcelAsync_RejectsNonExcelPath()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"dbp-export-{Guid.NewGuid():N}.pdf");
        var service = new TestExportService(filePath);

        Func<Task> act = async () => await service.ExportToExcelAsync(
            new[] { new CsvFormulaRow("value") },
            "wrong-extension");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*xlsx*");
    }

    [Fact]
    public async Task OpenExportedFileAsync_RejectsUnsafeExtension()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"dbp-export-{Guid.NewGuid():N}.ps1");
        await File.WriteAllTextAsync(filePath, "Write-Host unsafe");
        var service = new TestExportService(null);

        try
        {
            var result = await service.OpenExportedFileAsync(filePath);

            result.Should().BeFalse();
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private sealed record CsvFormulaRow(string Value);

    private sealed class CsvImportRow
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class TestExportService(string? filePath) : ExportService
    {
        public override string? ShowSaveFileDialog(string defaultFileName, string filter)
        {
            return filePath;
        }
    }
}
