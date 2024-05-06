namespace FluentImporter.Exceptions;

public class InvalidColumnValueException : ImportException
{
    public InvalidColumnValueException(string message, string? value = null) : base(message, value)
    {
    }
}