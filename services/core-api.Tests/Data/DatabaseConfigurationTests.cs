using Microsoft.Extensions.Configuration;
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
            DatabaseConfiguration.GetConnectionString(configuration));
    }
}
