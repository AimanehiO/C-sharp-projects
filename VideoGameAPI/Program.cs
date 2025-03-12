using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using VideoGameAPI.Data;
using StackExchange.Redis;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using VideoGameAPI.AppCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//configure serilog
builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration));
        

// Add services to the container.
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(
    builder.Configuration.GetConnectionString("Redis")));

builder.Services.AddScoped<ICacheService, RedisCacheService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<VideoGameDbContext>(options =>options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");

});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
