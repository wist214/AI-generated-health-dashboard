using HealthAggregatorV2.Api.Application.DTOs.Responses;

namespace HealthAggregatorV2.Api.Application.Services.Interfaces;

/// <summary>
/// Service for managing data source status.
/// </summary>
public interface ISourceStatusService
{
    /// <summary>
    /// Gets status information for all data sources.
    /// </summary>
    Task<IEnumerable<SourceStatusDto>> GetAllSourceStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets status information for a specific source.
    /// </summary>
    Task<SourceStatusDto?> GetSourceStatusAsync(string sourceName, CancellationToken cancellationToken = default);
}
