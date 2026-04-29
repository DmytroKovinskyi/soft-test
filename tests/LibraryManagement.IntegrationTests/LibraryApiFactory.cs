using LibraryManagement.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace LibraryManagement.IntegrationTests;

public class LibraryApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("library_test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); // Use a specific environment for tests
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LibraryDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add DbContext using Testcontainers connection string
            services.AddDbContext<LibraryDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Ensure database is created and seeded
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        await context.Database.EnsureCreatedAsync();
        await SeedDataHelper.SeedAsync(context);
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
