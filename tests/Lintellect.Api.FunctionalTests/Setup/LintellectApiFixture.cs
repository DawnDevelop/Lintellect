using System.Data.Common;
using Lintellect.Api.FunctionalTests.Mocks.AI;
using Lintellect.Api.FunctionalTests.Mocks.Git;
using Lintellect.Api.Infrastructure.Persistence;
using Lintellect.Api.Infrastructure.Services.Analysis;
using Lintellect.Api.Infrastructure.Services.Webhooks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;

namespace Lintellect.Api.FunctionalTests.Setup;

/// <summary>
/// Custom WebApplicationFactory for functional tests.
/// </summary>
public sealed class LintellectApiFixture : WebApplicationFactory<Program>
{
    private readonly IHost _app;
    public IResourceBuilder<PostgresServerResource> Postgres { get; private set; }
    public string? PostgresConnectionString { get; set; }
    private DbConnection _connection = null!;
    private static Respawner _respawner = null!;

    public const string API_KEY = "test-api-key";

    public LintellectApiFixture()
    {
        var options = new DistributedApplicationOptions { AssemblyName = typeof(LintellectApiFixture).Assembly.FullName, DisableDashboard = true };
        var appBuilder = DistributedApplication.CreateBuilder(options);

        Postgres = appBuilder.AddPostgres("postgresDb")
            .WithHostPort(5432)
            .WithExternalHttpEndpoints();
        //.WithEndpoint(name: "postgresendpoint", scheme: "tcp", port: 5433, targetPort: 5433, isProxied: false);

        _app = appBuilder.Build();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"ConnectionStrings:{Postgres.Resource.Name}", PostgresConnectionString },
                { "ApiKey", API_KEY  }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real database context
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add test database context
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(PostgresConnectionString));

            // Replace external services with mocks
            services.AddScoped<IGitClientFactory, MockGitClientFactory>();
            services.AddScoped<IAnalyzerServiceResolver, MockAnalyzerServiceResolver>();

            // Remove background services for testing to avoid race conditions
            var analysisBackgroundService = services.FirstOrDefault(s => s.ImplementationType == typeof(AnalysisBackgroundService));
            if (analysisBackgroundService != null)
            {
                services.Remove(analysisBackgroundService);
            }

            var webhookBackgroundService = services.FirstOrDefault(s => s.ImplementationType == typeof(WebhookBackgroundService));
            if (webhookBackgroundService != null)
            {
                services.Remove(webhookBackgroundService);
            }
        });

        builder.UseEnvironment("Testing");

        return base.CreateHost(builder);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _app.StopAsync();
        if (_app is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _app.Dispose();
        }

        await _connection.DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        await _app.StartAsync();

        // Wait for Postgres to be ready - GetConnectionStringAsync may return null initially
        // so we retry until we get a valid connection string and can actually connect
        PostgresConnectionString = await WaitForPostgresConnectionStringAsync();
    }

    private async Task<string> WaitForPostgresConnectionStringAsync()
    {
        const int maxRetries = 30;
        const int delayMs = 1000;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var connectionString = await Postgres.Resource.GetConnectionStringAsync();
                if (connectionString != null)
                {
                    // Verify we can actually connect to ensure it's ready
                    using var testConnection = new NpgsqlConnection(connectionString);
                    await testConnection.OpenAsync();
                    await testConnection.CloseAsync();
                    return connectionString;
                }
            }
            catch
            {
                // Connection string not available yet or connection failed, retry
            }

            await Task.Delay(delayMs);
        }

        throw new TimeoutException("Postgres resource did not become ready within the timeout period");
    }

    public async Task InitializeDbConnectionAsync()
    {
        _connection = new NpgsqlConnection(PostgresConnectionString);

        await _connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_connection,
            new RespawnerOptions { DbAdapter = DbAdapter.Postgres });
        await _connection.CloseAsync();
    }

    public async Task ResetAsync()
    {
        await _connection.OpenAsync();
        await _respawner.ResetAsync(_connection);
        await _connection.CloseAsync();
    }
}
