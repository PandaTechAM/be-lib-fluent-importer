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
    ///     <para>
    ///         A label that adds nothing is dropped: a value set whose values are already words (<c>Yes</c>/<c>No</c>)
    ///         renders its own label once the template is generated in the language those words came from, and
    ///         <c>"Yes - Yes"</c> reads like a defect. The reader accepts a bare value, so collapsing it is safe.
    ///     </para>
    /// </summary>
    public string DropdownText => string.IsNullOrWhiteSpace(Label)
                                  || string.Equals(Value, Label, StringComparison.OrdinalIgnoreCase)
        ? Value
        : $"{Value}{ImportTemplateSeparators.CodeLabel}{Label}";
}
