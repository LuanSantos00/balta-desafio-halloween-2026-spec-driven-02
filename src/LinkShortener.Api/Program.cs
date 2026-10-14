using LinkShortener.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Links") ?? "Data Source=links.db";

builder.Services.AddDbContext<LinkDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

app.Run();
