namespace FluentImporter.Exceptions;

/// <summary>
///     Thrown when two header cells normalize to the same column name. Previously surfaced as a raw
///     <see cref="ArgumentException" /> that escaped the row-error wrapper.
/// </summary>
public class DuplicateColumnException(string message, string? messageDetails = null)
    : ImportException(message, messageDetails);
