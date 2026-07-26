namespace FluentImporter.Templates;

/// <summary>
///     Every caption the generated template renders. Set these to localize the legend; the data sheet's header row is
///     never localized because it is the parsing contract.
/// </summary>
public sealed class ImportTemplateTexts
{
    /// <summary>Title cell at the top of the legend sheet.</summary>
    public string LegendTitle { get; set; } = "Legend";

    /// <summary>Caption above the column-reference table.</summary>
    public string ColumnsTitle { get; set; } = "Columns";

    /// <summary>Caption above the value tables.</summary>
    public string AllowedValuesTitle { get; set; } = "Allowed values";

    /// <summary>Column-reference table headers.</summary>
    public string ColumnHeader { get; set; } = "Column";

    /// <inheritdoc cref="ColumnHeader" />
    public string RequiredHeader { get; set; } = "Required";

    /// <inheritdoc cref="ColumnHeader" />
    public string TypeHeader { get; set; } = "Type";

    /// <inheritdoc cref="ColumnHeader" />
    public string FormatHeader { get; set; } = "Format";

    /// <inheritdoc cref="ColumnHeader" />
    public string DescriptionHeader { get; set; } = "Description";

    /// <summary>Value-table headers.</summary>
    public string ValueHeader { get; set; } = "Value";

    /// <inheritdoc cref="ValueHeader" />
    public string LabelHeader { get; set; } = "Description";

    /// <summary>Rendered in the Required column.</summary>
    public string RequiredYes { get; set; } = "Yes";

    /// <inheritdoc cref="RequiredYes" />
    public string RequiredNo { get; set; } = "No";

    /// <summary>Friendly type names shown in the legend.</summary>
    public string WholeNumberType { get; set; } = "Whole number";

    /// <inheritdoc cref="WholeNumberType" />
    public string DecimalNumberType { get; set; } = "Decimal number";

    /// <inheritdoc cref="WholeNumberType" />
    public string TextType { get; set; } = "Text";

    /// <inheritdoc cref="WholeNumberType" />
    public string DateType { get; set; } = "Date";

    /// <inheritdoc cref="WholeNumberType" />
    public string BooleanType { get; set; } = "Yes / No";

    /// <inheritdoc cref="WholeNumberType" />
    public string ListType { get; set; } = "Pick from list";

    /// <summary>Dropdown validation prompt shown when the operator types something not in the list.</summary>
    public string InvalidValueTitle { get; set; } = "Invalid value";

    /// <inheritdoc cref="InvalidValueTitle" />
    public string InvalidValueMessage { get; set; } = "Pick a value from the list. See the legend sheet.";

    /// <summary>Note attached to required header cells.</summary>
    public string RequiredNote { get; set; } = "Required";
}
