namespace FluentImporter.Exceptions;

/// <summary>
///     Thrown when a worksheet was requested by name and the workbook does not contain it.
/// </summary>
public class SheetNotFoundException(string message, string? messageDetails = null)
    : ImportException(message, messageDetails);
