namespace FluentImporter.Exceptions;

public class InvalidCellValueException(string message, string? value = null)
    : ImportException(message, value);