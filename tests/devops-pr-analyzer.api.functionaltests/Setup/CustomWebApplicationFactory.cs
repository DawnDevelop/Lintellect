using devops_pr_analyzer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace devops_pr_analyzer.api.functionaltests.Setup;

/// <summary>
/// Custom WebApplicationFactory for functional tests.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real database context
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add test database context
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_connectionString);
            });

            // Replace external services with mocks
            services.AddScoped<IGitClientFactory, MockGitClientFactory>();
            services.AddScoped<IAnalyzerServiceResolver, MockAnalyzerServiceResolver>();
        });

        builder.UseEnvironment("Testing");
    }
}
