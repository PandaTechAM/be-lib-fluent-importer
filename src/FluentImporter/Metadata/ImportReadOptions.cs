namespace FluentImporter.Metadata;

/// <summary>
///     Options for reading a file.
/// </summary>
public sealed class ImportReadOptions
{
    /// <summary>
    ///     Worksheet to read, by name. When null the first sheet by tab position is used, which means a legend or
    ///     instruction sheet must not be placed first. Name it explicitly whenever the layout is known.
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>Stop collecting after this many cell errors. Ignored by the throwing overloads.</summary>
    public int MaxErrors { get; set; } = 100;
}
