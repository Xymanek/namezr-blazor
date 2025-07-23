namespace Namezr.Features.SelectionSeries.Data;

/// <summary>
/// Indicates how a selection batch was created.
/// Values must never change as they are stored in the database.
/// </summary>
public enum SelectionBatchType
{
    /// <summary>
    /// Batch was created automatically by the selection worker.
    /// </summary>
    Automatic = 0,
    
    /// <summary>
    /// Batch was created manually by a staff member.
    /// </summary>
    Manual = 1,
}