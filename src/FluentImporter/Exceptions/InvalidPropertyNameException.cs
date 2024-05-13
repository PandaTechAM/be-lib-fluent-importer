namespace FluentImporter.Exceptions;

public class InvalidPropertyNameException(string message, string? messageDetails = null)
    : ImportException(message, messageDetails);