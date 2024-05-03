namespace FluentImporter.Exceptions;

public class InvalidColumnValueException : ImportException
{
    public InvalidColumnValueException(string message, string value) : base(message, value)
    {
    }

    public InvalidColumnValueException()
    {
    }
}