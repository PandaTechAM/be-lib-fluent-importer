namespace FluentImporter.Exceptions;

public class InvalidColumnValueException(string message, string? messageDetails = null)
   : ImportException(message, messageDetails);