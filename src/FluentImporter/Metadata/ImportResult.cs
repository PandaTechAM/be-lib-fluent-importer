namespace FluentImporter.Metadata;

/// <summary>
///     Outcome of a non-throwing read: the rows that parsed plus every row that did not.
/// </summary>
public sealed class ImportResult<TModel>(List<TModel> records, IReadOnlyList<ImportError> errors)
    where TModel : class
{
    /// <summary>Rows that parsed successfully.</summary>
    public List<TModel> Records { get; } = records;

    /// <summary>Every cell failure encountered, capped by <see cref="ImportReadOptions.MaxErrors" />.</summary>
    public IReadOnlyList<ImportError> Errors { get; } = errors;

    /// <summary>True when at least one row failed.</summary>
    public bool HasErrors => Errors.Count > 0;
}
