namespace FluentImporter.Exceptions;

/// <summary>
///     Thrown when a rule references a property that does not exist on the model.
/// </summary>
public class InvalidPropertyNameException(string message, string? messageDetails = null)
    : ImportException(message, messageDetails);
