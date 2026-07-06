namespace FluentImporter.Exceptions;

/// <summary>
///     Thrown when a cell value fails validation or conversion for the target property.
/// </summary>
public class InvalidCellValueException(string message, string? messageDetails = null)
    : ImportException(message, messageDetails);
