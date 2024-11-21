namespace FluentImporter.Exceptions;

public class InvalidCellValueException(string message, string? messageDetails = null)
   : ImportException(message, messageDetails);