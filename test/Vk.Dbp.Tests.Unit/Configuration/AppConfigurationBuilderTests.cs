using Dabp.WpfWindow.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Configuration;

public sealed class AppConfigurationBuilderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string? _originalConnectionString;

    public AppConfigurationBuilderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"dbp-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        _originalConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", null);
    }

    [Fact]
    public void Build_LoadsLocalConfigurationOverApplicationDefaults()
    {
        WriteJson("appsettings.json", """
        {
          "ConnectionStrings": {
            "Default": "Server=base;"
          },
          "Session": {
            "TimeoutMinutes": 15
          }
        }
        """);
        WriteJson("appsettings.local.json", """
        {
          "ConnectionStrings": {
            "Default": "Server=local;"
          },
          "Session": {
            "TimeoutMinutes": 30
          }
        }
        """);

        IConfigurationRoot configuration = AppConfigurationBuilder.Build(_tempDirectory);

        configuration.GetConnectionString("Default").Should().Be("Server=local;");
        configuration.GetValue<int>("Session:TimeoutMinutes").Should().Be(30);
    }

    [Fact]
    public void Build_DoesNotLoadExampleConfigurationAsRuntimeConfiguration()
    {
        WriteJson("appsettings.json", """
        {
          "ConnectionStrings": {
            "Default": ""
          }
        }
        """);
        WriteJson("appsettings.local.example.json", """
        {
          "ConnectionStrings": {
            "Default": "Server=example;"
          }
        }
        """);

        IConfigurationRoot configuration = AppConfigurationBuilder.Build(_tempDirectory);

        configuration.GetConnectionString("Default").Should().BeEmpty();
    }

    [Fact]
    public void Build_AllowsEnvironmentVariablesToOverrideLocalConfiguration()
    {
        WriteJson("appsettings.json", """
        {
          "ConnectionStrings": {
            "Default": "Server=base;"
          }
        }
        """);
        WriteJson("appsettings.local.json", """
        {
          "ConnectionStrings": {
            "Default": "Server=local;"
          }
        }
        """);
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Server=environment;");

        IConfigurationRoot configuration = AppConfigurationBuilder.Build(_tempDirectory);

        configuration.GetConnectionString("Default").Should().Be("Server=environment;");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _originalConnectionString);

        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private void WriteJson(string fileName, string json)
    {
        File.WriteAllText(Path.Combine(_tempDirectory, fileName), json);
    }
}
