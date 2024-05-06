namespace FluentImporter.Exceptions;

public class EmptyFileImportException(string message, string? value = null)
    : ImportException(message, value);