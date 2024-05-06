namespace FluentImporter.Exceptions;

public class InvalidPropertyNameException(string message, string? value = null)
    : ImportException(message, value);