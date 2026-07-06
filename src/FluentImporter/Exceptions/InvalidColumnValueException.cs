namespace FluentImporter.Exceptions;

/// <summary>
///     Thrown when a mapped column is missing or its value does not satisfy the rule.
/// </summary>
public class InvalidColumnValueException(string message, string? messageDetails = null)
    : ImportException(message, messageDetails);
