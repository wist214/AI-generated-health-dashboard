using HealthAggregatorV2.Api.Extensions;
using HealthAggregatorV2.Api.Middleware;
using HealthAggregatorV2.Infrastructure.Data;
using HealthAggregatorV2.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== Database =====
builder.Services.AddHealthDatabase(builder.Configuration);

// ===== Repositories =====
builder.Services.AddRepositories();

// ===== Application Services =====
builder.Services.AddApplicationServices();

// ===== API Documentation =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Health Aggregator API",
        Version = "v1",
        Description = "API for aggregating health data from multiple sources"
    });
});

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaPolicy", policy =>
    {
        var spaOrigin = builder.Configuration["AllowedOrigins:SPA"] ?? "http://localhost:3000";
        var swaOrigin = builder.Configuration["AllowedOrigins:SWA"] ?? "https://*.azurestaticapps.net";

        policy
            .WithOrigins(spaOrigin, swaOrigin)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ===== Application Insights =====
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// ===== Health Checks =====
builder.Services.AddHealthChecks()
    .AddDbContextCheck<HealthDbContext>();

// ===== Build Application =====
var app = builder.Build();

// ===== Middleware Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Health Aggregator API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("SpaPolicy");

// Custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// Health checks
app.MapHealthChecks("/health");

// Map API endpoints
app.MapApiEndpoints();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
