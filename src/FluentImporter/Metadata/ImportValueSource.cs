namespace FluentImporter.Metadata;

/// <summary>
///     Where a column's value comes from.
/// </summary>
public enum ImportValueSource
{
    /// <summary>Read from a column in the imported file. Appears in generated templates.</summary>
    Column = 0,

    /// <summary>A constant supplied by <c>WriteValue</c>. Never read from the file, never templated.</summary>
    Constant = 1,

    /// <summary>Computed from the model by <c>ReadFromModel</c>. Never read from the file, never templated.</summary>
    Computed = 2
}
