namespace FluentImporter.Metadata;

/// <summary>
///     One entry of a closed value set for a column: the value written to the cell and the label shown in the legend.
/// </summary>
/// <param name="Value">The literal value the importer expects in the cell (e.g. <c>"0"</c>).</param>
/// <param name="Label">Human-readable label for the legend and the dropdown (e.g. <c>"Active"</c>).</param>
public sealed record AllowedValue(string Value, string Label)
{
    /// <summary>
    ///     The text shown in the generated dropdown. The importer strips the label back off on read, so the operator
    ///     picks something meaningful while the cell still carries the value.
    /// </summary>
    public string DropdownText => $"{Value}{ImportTemplateSeparators.CodeLabel}{Label}";
}
