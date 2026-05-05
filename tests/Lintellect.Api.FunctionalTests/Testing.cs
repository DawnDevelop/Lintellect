using Lintellect.Api.FunctionalTests.Setup;
using Lintellect.Api.Infrastructure.Persistence;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Lintellect.Api.FunctionalTests;


[SetUpFixture]
public partial class Testing
{
    public static HttpClient Client { get; set; }
    public static LintellectApiFixture WebApplicationFactory { get; set; }

    public static string PostgresConnectionString { get; set; }

    private static IServiceScopeFactory _scopeFactory = null!;

    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {
        WebApplicationFactory = new LintellectApiFixture();
        await WebApplicationFactory.InitializeAsync();

        Client = WebApplicationFactory.CreateClient();
        Client.DefaultRequestHeaders.Add("Api-Key", LintellectApiFixture.API_KEY);

        await WebApplicationFactory.InitializeDbConnectionAsync();


        PostgresConnectionString = WebApplicationFactory.PostgresConnectionString!;
        _scopeFactory = WebApplicationFactory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    [OneTimeTearDown]
    public async Task DisposeAsync()
    {
        Client?.Dispose();

        await WebApplicationFactory.DisposeAsync();
    }

    public static async Task<T> GetService<T>() where T : notnull
    {
        using var scope = WebApplicationFactory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    public static (IServiceScope Scope, ApplicationDbContext Context) GetDbContext()
    {
        var scope = WebApplicationFactory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (scope, context);
    }

    public static async Task ResetStateAsync()
    {
        await WebApplicationFactory.ResetAsync();
    }

    public static async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _scopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        return await mediator.Send(request);
    }
}
