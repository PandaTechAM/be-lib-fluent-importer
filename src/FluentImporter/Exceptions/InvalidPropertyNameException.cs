namespace FluentImporter.Exceptions;

public class InvalidPropertyNameException : ImportException
{
    public InvalidPropertyNameException(string message, string value) : base(message, value)
    {
    }

    public InvalidPropertyNameException()
    {
    }
}