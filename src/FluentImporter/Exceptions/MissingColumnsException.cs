namespace FluentImporter.Exceptions;

/// <summary>
///     Thrown up front when required columns are absent from the header row, so a wrong file reports "these columns are
///     missing" instead of a misleading cell error on row 2.
/// </summary>
public class MissingColumnsException(string message, string? messageDetails = null)
    : ImportException(message, messageDetails)
{
    /// <summary>The configured names of the columns that were not found.</summary>
    public IReadOnlyList<string> MissingColumns { get; init; } = [];
}
