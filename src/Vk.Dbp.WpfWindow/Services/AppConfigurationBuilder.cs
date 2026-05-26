using System;
using Microsoft.Extensions.Configuration;

namespace Dabp.WpfWindow.Services;

public static class AppConfigurationBuilder
{
    public static IConfigurationRoot Build()
    {
        return Build(AppContext.BaseDirectory);
    }

    public static IConfigurationRoot Build(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Configuration base path is required.", nameof(basePath));
        }

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
