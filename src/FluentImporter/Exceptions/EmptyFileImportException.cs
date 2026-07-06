namespace FluentImporter.Exceptions;

/// <summary>
///     Thrown when an imported file contains no data rows.
/// </summary>
public class EmptyFileImportException(string message, string? messageDetails = null)
    : ImportException(message, messageDetails);
