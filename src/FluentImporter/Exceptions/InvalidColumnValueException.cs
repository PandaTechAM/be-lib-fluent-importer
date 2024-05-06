namespace FluentImporter.Exceptions;

public class InvalidColumnValueException(string message, string? value = null)
    : ImportException(message, value);