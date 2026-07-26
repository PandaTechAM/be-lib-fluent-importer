namespace FluentImporter.Metadata;

/// <summary>
///     One cell-level failure, as structured data rather than only a formatted message.
/// </summary>
/// <param name="RowNumber">1-based row number in the source file; null when reading in-memory rows.</param>
/// <param name="ColumnName">Configured column name that failed.</param>
/// <param name="PropertyName">Target property on the model.</param>
/// <param name="RawValue">Raw cell text, truncated; <c>"null"</c> when the cell was empty or absent.</param>
/// <param name="TargetType">Name of the target property type.</param>
/// <param name="Message">Innermost failure message.</param>
public sealed record ImportError(
    int? RowNumber,
    string ColumnName,
    string PropertyName,
    string RawValue,
    string TargetType,
    string Message)
{
    /// <summary>
    ///     Formatted single-line message. Kept stable so callers matching on the message keep working.
    /// </summary>
    public override string ToString()
    {
        var row = RowNumber.HasValue ? $"row {RowNumber.Value}" : "row ?";
        return $"Invalid cell value at {row}, column '{ColumnName}', property '{PropertyName}', "
               + $"raw '{RawValue}', target '{TargetType}'. Error: {Message}";
    }
}
