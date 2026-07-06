namespace FluentImporter.Exceptions;

/// <summary>
///     Base type for all import failures. Carries optional structured detail alongside the message.
/// </summary>
public abstract class ImportException(string message, string? messageDetails = null)
    : Exception(message)
{
    /// <summary>
    ///     Optional structured detail about the failure (e.g. the offending column and raw value).
    /// </summary>
    public string? MessageDetails { get; private set; } = messageDetails;
}
