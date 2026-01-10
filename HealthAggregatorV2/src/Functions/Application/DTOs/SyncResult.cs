namespace HealthAggregatorV2.Functions.Application.DTOs;

/// <summary>
/// Result of a sync operation.
/// </summary>
public class SyncResult
{
    public bool IsSuccess { get; set; }
    public int RecordsSynced { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    // Aggregate counts for orchestrator
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalRecordsSynced { get; set; }

    public static SyncResult Success(int recordsSynced, Dictionary<string, object>? metadata = null) =>
        new()
        {
            IsSuccess = true,
            RecordsSynced = recordsSynced,
            CompletedAt = DateTime.UtcNow,
            Metadata = metadata
        };

    public static SyncResult Failure(string errorMessage) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            CompletedAt = DateTime.UtcNow
        };
}
