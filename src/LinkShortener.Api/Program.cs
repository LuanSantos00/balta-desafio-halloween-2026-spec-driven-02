using LinkShortener.Api.Application;
using LinkShortener.Api.Data;
using LinkShortener.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var app = Program.CreateApp(args);
await app.RunAsync();

public partial class Program
{
    public static WebApplication CreateApp(string[] args, Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        configureBuilder?.Invoke(builder);

        var connectionString = builder.Configuration.GetConnectionString("Links") ?? "Data Source=links.db";
        builder.Services.AddDbContext<LinkDbContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddScoped<ILinkRepository, LinkRepository>();
        builder.Services.AddScoped<LinkService>();
        builder.Services.AddSingleton<ShortCodeGenerator>();
        builder.Services.AddSingleton<TargetUrlValidator>();

        var app = builder.Build();
        app.MapLinksEndpoints();

        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<LinkDbContext>().Database.Migrate();

        return app;
    }
}
