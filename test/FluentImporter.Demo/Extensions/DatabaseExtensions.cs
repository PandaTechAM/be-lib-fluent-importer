using FluentImporter.Demo.Context;
using Microsoft.EntityFrameworkCore;

namespace FluentImporter.Demo.Extensions;

public static class DatabaseExtensions
{
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<MyDbContext>(options => { options.UseSqlite("Data Source=file_importer.db"); });
        return builder;
    }

    public static WebApplication DeleteDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<MyDbContext>();
        context.Database.EnsureDeleted();
        return app;
    }

    public static WebApplication CreateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<MyDbContext>();
        context.Database.EnsureCreated();
        return app;
    }
}