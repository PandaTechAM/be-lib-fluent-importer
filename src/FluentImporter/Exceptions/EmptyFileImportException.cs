namespace FluentImporter.Exceptions;

public class EmptyFileImportException : ImportException
{
    public EmptyFileImportException(string message, string? value = null) : base(message, value)
    {
    }
}