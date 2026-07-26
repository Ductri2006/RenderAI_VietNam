using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RenderVN.CoreApi.Data;

namespace RenderVN.CoreApi.Tests.Data;

public sealed class DatabaseConfigurationTests
{
    [Fact]
    public void EnvironmentConnectionTakesPrecedenceOverDefaultConnection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_CONNECTION"] = "Host=env;Database=production",
                ["ConnectionStrings:DefaultConnection"] = "Host=local;Database=local"
            })
            .Build();

        Assert.Equal(
            "Host=env;Database=production",
            DatabaseConfiguration.GetConnectionString(
                configuration,
                new TestEnvironment(Environments.Production)));
    }

    [Fact]
    public void DefaultConnectionIsOnlyAcceptedInDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=local;Database=local"
            })
            .Build();

        Assert.Equal(
            "Host=local;Database=local",
            DatabaseConfiguration.GetConnectionString(
                configuration,
                new TestEnvironment(Environments.Development)));
        Assert.Throws<InvalidOperationException>(() =>
            DatabaseConfiguration.GetConnectionString(
                configuration,
                new TestEnvironment(Environments.Production)));
    }

    private sealed class TestEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "RenderVN.CoreApi.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
