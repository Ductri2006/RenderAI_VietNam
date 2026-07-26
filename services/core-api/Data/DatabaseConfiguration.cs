using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace RenderVN.CoreApi.Data;

public static class DatabaseConfiguration
{
    public static string GetConnectionString(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var environmentConnection = configuration["DATABASE_CONNECTION"];
        if (!string.IsNullOrWhiteSpace(environmentConnection))
        {
            return environmentConnection;
        }

        if (environment.IsDevelopment())
        {
            return configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection is required in Development.");
        }

        throw new InvalidOperationException(
            "DATABASE_CONNECTION is required outside Development.");
    }
}
