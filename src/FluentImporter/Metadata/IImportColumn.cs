namespace FluentImporter.Metadata;

/// <summary>
///     Read-only view of one configured column. Exposing this is what lets a template be generated from the import
///     rule itself, so the file the operator downloads cannot drift from the file the parser accepts.
/// </summary>
public interface IImportColumn
{
    /// <summary>Name of the property on the model.</summary>
    string PropertyName { get; }

    /// <summary>Header text the importer looks for. Matching is case- and outer-whitespace-insensitive.</summary>
    string ColumnName { get; }

    /// <summary>Additional accepted header spellings, from <c>WithAliases</c>.</summary>
    IReadOnlyList<string> Aliases { get; }

    /// <summary>The property type with <see cref="Nullable{T}" /> unwrapped.</summary>
    Type PropertyType { get; }

    /// <summary>True when the property is a nullable value type or a nullable reference type.</summary>
    bool IsNullable { get; }

    /// <summary>True when <c>NotEmpty</c> was configured. A required column must be present in the file.</summary>
    bool IsRequired { get; }

    /// <summary>Value used when the cell is empty, from <c>Default</c>.</summary>
    object? DefaultValue { get; }

    /// <summary>Validation pattern from <c>Validate</c>; <c>".*"</c> when unset.</summary>
    string RegexPattern { get; }

    /// <summary>True when a custom <c>Convert</c> delegate is configured.</summary>
    bool HasCustomConverter { get; }

    /// <summary>Human description for the legend, from <c>Describe</c>.</summary>
    string? Description { get; }

    /// <summary>
    ///     Expected textual format, from <c>ExpectedFormat</c> (e.g. <c>"dd/MM/yyyy"</c>). Declare it whenever a custom
    ///     converter expects a specific shape — the delegate itself is opaque and cannot be inspected.
    /// </summary>
    string? ExpectedFormat { get; }

    /// <summary>Example cell value for generated sample rows, from <c>Example</c>.</summary>
    object? Example { get; }

    /// <summary>
    ///     The enum this column carries, from <c>EnumSource</c>. Set this on <c>int</c> columns that hold enum values:
    ///     the property type alone cannot reveal it.
    /// </summary>
    Type? EnumSource { get; }

    /// <summary>Explicitly declared closed value set, from <c>AllowedValues</c>.</summary>
    IReadOnlyList<AllowedValue> AllowedValues { get; }

    /// <summary>Where this column's value comes from.</summary>
    ImportValueSource ValueSource { get; }

    /// <summary>True when the column has either an <see cref="EnumSource" /> or explicit <see cref="AllowedValues" />.</summary>
    bool HasValueSet { get; }
}
