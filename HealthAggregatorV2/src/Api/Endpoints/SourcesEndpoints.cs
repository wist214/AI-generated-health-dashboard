using HealthAggregatorV2.Api.Application.Services.Interfaces;

namespace HealthAggregatorV2.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for data source status operations.
/// </summary>
public static class SourcesEndpoints
{
    public static void MapSourcesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sources")
            .WithTags("Sources")
            .WithOpenApi();

        group.MapGet("/", GetAllSourceStatus)
            .WithName("GetAllSourceStatus")
            .WithDescription("Gets status information for all data sources");

        group.MapGet("/{sourceName}", GetSourceStatus)
            .WithName("GetSourceStatus")
            .WithDescription("Gets status information for a specific source");
    }

    private static async Task<IResult> GetAllSourceStatus(
        ISourceStatusService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAllSourceStatusAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetSourceStatus(
        string sourceName,
        ISourceStatusService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetSourceStatusAsync(sourceName, cancellationToken);
        return result is null
            ? Results.NotFound($"Source '{sourceName}' not found")
            : Results.Ok(result);
    }
}
