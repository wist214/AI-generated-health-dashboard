namespace HealthAggregatorV2.Domain.Common;

/// <summary>
/// Base entity class providing common audit properties for all entities.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// UTC timestamp when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the entity was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
