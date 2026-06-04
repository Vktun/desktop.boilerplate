using Dabp.WpfWindow.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Configuration;

public sealed class ConfigurationValidatorTests
{
    [Fact]
    public void Validate_AllowsDisabledRedisWithoutConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=test;",
                ["Redis:Enabled"] = "false"
            })
            .Build();

        Action act = () => ConfigurationValidator.Validate(configuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ThrowsWhenRedisEnabledWithoutConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=test;",
                ["Redis:Enabled"] = "true"
            })
            .Build();

        Action act = () => ConfigurationValidator.Validate(configuration);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Redis:ConnectionString*");
    }
}
