using FluentImporter.Metadata;

namespace FluentImporter.Templates;

/// <summary>
///     Shape and wording of a generated import template. The library knows the columns; the caller supplies the
///     language, so nothing about localization leaks in here.
/// </summary>
public sealed class ImportTemplateOptions
{
    /// <summary>
    ///     Name of the fillable sheet. It is always written first, so it is sheet 1 and the tab Excel opens on.
    /// </summary>
    public string DataSheetName { get; set; } = "Data";

    /// <summary>Name of the legend sheet, always written second.</summary>
    public string LegendSheetName { get; set; } = "Legend";

    /// <summary>
    ///     How many empty rows below the sample rows get dropdowns and number formats, so the operator can keep typing.
    /// </summary>
    public int BlankRowsForInput { get; set; } = 500;

    /// <summary>
    ///     Add an in-cell dropdown to every column that has a value set, sourced from the legend sheet.
    /// </summary>
    public bool AddDropdowns { get; set; } = true;

    /// <summary>
    ///     Resolves the label for one enum member: <c>(enumType, memberName, numericValue) =&gt; label</c>.
    ///     Falls back to the member name when null or when it returns null / whitespace.
    /// </summary>
    public Func<Type, string, long, string?>? EnumLabelResolver { get; set; }

    /// <summary>
    ///     Overrides a column's legend description, so descriptions can be localized without touching the rule.
    ///     Falls back to <see cref="IImportColumn.Description" /> when null or when it returns null / whitespace.
    /// </summary>
    public Func<IImportColumn, string?>? ColumnDescriptionResolver { get; set; }

    /// <summary>All rendered captions.</summary>
    public ImportTemplateTexts Texts { get; set; } = new();
}
