namespace FluentImporter.Exceptions;

public class EmptyFileImportException(string message, string? messageDetails = null)
    : ImportException(message, messageDetails);