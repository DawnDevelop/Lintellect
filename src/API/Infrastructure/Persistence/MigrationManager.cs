using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace devops_pr_analyzer.Infrastructure.Persistence;

public static class MigrationManager
{
    public static async Task MigrateAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();


        //await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
}
