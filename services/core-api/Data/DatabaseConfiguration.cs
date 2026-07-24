using Microsoft.Extensions.Configuration;

namespace RenderVN.CoreApi.Data;

public static class DatabaseConfiguration
{
    public static string GetConnectionString(IConfiguration configuration)
    {
        return configuration["DATABASE_CONNECTION"]
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DATABASE_CONNECTION or ConnectionStrings:DefaultConnection is required.");
    }
}
