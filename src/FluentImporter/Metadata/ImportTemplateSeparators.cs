namespace FluentImporter.Metadata;

/// <summary>
///     Separators shared by the template writer and the reader. They are the contract that lets a generated dropdown
///     show <c>"0 - Active"</c> while the column still imports as <c>0</c>.
/// </summary>
public static class ImportTemplateSeparators
{
    /// <summary>Separates a value from its label in a dropdown cell.</summary>
    public const string CodeLabel = " - ";
}
