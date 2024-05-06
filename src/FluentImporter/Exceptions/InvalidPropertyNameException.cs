namespace FluentImporter.Exceptions;

public class InvalidPropertyNameException : ImportException
{
    public InvalidPropertyNameException(string message, string? value = null) : base(message, value)
    {
    }
}