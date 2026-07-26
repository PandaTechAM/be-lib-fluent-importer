using FluentImporter.Metadata;

namespace FluentImporter.Exceptions;

/// <summary>
///     Thrown when a cell value fails validation or conversion for the target property.
/// </summary>
public class InvalidCellValueException(string message, string? messageDetails = null, ImportError? error = null)
    : ImportException(message, messageDetails)
{
    /// <summary>Structured detail of the failing cell, when the failure originated from a row read.</summary>
    public ImportError? Error { get; } = error;
}
