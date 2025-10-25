using devops_pr_analyzer.api.functionaltests.Setup;
using devops_pr_analyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;

namespace devops_pr_analyzer.api.functionaltests;

/// <summary>
/// Base class for all functional tests.
/// </summary>
[SetUpFixture]
public abstract class Testing
{
    public HttpClient Client { get; set; }
    public CustomWebApplicationFactory Factory { get; set; }

    private static Respawner _respawner;

    private static string ConnectionString { get; set; }

    [OneTimeSetUp]
    public async Task InitializeAsync()
    {
        var db = new TestDatabase();
        await db.InitializeAsync();

        // Reset database state before each test
        _respawner = await Respawner.CreateAsync(
            db.ConnectionString,
            new RespawnerOptions
            {
                TablesToIgnore = ["__EFMigrationsHistory"]
            });
        ConnectionString = db.ConnectionString;
        Factory = new CustomWebApplicationFactory(db.ConnectionString);
        Client = Factory.CreateClient();

        await _respawner.ResetAsync(db.ConnectionString);
    }

    [OneTimeTearDown]
    public Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<T> GetService<T>() where T : notnull
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    protected async Task<ApplicationDbContext> GetDbContext()
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public static async Task ResetStateAsync()
    {
        await _respawner.ResetAsync(ConnectionString);
    }
}
