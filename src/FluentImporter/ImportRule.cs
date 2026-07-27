using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using FluentImporter.Enums;
using FluentImporter.Exceptions;
using FluentImporter.Metadata;
using FluentImporter.Templates;

namespace FluentImporter;

/// <summary>
///     Base class for defining import rules for a model type. A rule is both the parser and, through
///     <see cref="BuildTemplate" />, the generator of the template the operator fills in — so the two cannot drift.
/// </summary>
/// <typeparam name="TModel">The model type to import data into.</typeparam>
public class ImportRule<TModel> where TModel : class
{
    private readonly List<IColumnBinding> _columns = [];

    /// <summary>
    ///     Ordered, read-only view of every configured column, in <c>RuleFor</c> declaration order.
    /// </summary>
    public IReadOnlyList<IImportColumn> Columns => _columns;

    /// <summary>
    ///     Define a rule for a specific property.
    /// </summary>
    protected PropertyRule<TProperty> RuleFor<TProperty>(Expression<Func<TModel, TProperty>> navigationPropertyPath)
    {
        if (navigationPropertyPath.Body is not MemberExpression memberExpression)
        {
            throw new InvalidPropertyNameException("Invalid property expression", string.Empty);
        }

        var property = typeof(TModel).GetProperty(memberExpression.Member.Name);
        if (property is null)
        {
            throw new InvalidPropertyNameException(
                $"Property '{memberExpression.Member.Name}' does not exist on {typeof(TModel).Name}",
                memberExpression.Member.Name);
        }

        var rule = new PropertyRule<TProperty>(property);
        _columns.Add(rule);
        return rule;
    }

    /// <summary>
    ///     Generate a two-sheet xlsx template for this rule: a fillable data sheet first, a legend second.
    /// </summary>
    /// <param name="examples">
    ///     Sample rows to render. When null or empty, one row is synthesized from each column's <c>Example</c>.
    ///     A column declaring <c>ExampleText</c> renders that text either way.
    /// </param>
    /// <param name="options">Sheet names, captions and the enum-label resolver. Defaults are English.</param>
    public byte[] BuildTemplate(IEnumerable<TModel>? examples = null, ImportTemplateOptions? options = null)
    {
        options ??= new ImportTemplateOptions();

        var templateColumns = _columns
            .Where(c => c.ValueSource == ImportValueSource.Column)
            .ToList();

        if (templateColumns.Count == 0)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} defines no readable columns, so no template can be generated.");
        }

        var rows = new List<object?[]>();

        foreach (var example in examples ?? [])
        {
            rows.Add(templateColumns.Select(c => c.ExampleText ?? c.Property.GetValue(example)).ToArray());
        }

        if (rows.Count == 0)
        {
            rows.Add(templateColumns.Select(c => c.ExampleText ?? c.Example).ToArray());
        }

        return ImportTemplateBuilder.Build(templateColumns, rows, options);
    }

    /// <summary>
    ///     Get records from in-memory dictionary data keyed by column name.
    /// </summary>
    public IEnumerable<TModel> GetRecords(IEnumerable<Dictionary<string, string>> data)
    {
        return data.Select(row => GetRecordByName(row, null, null)!);
    }

    /// <summary>
    ///     Read and import data from a CSV stream. Throws on the first bad cell.
    /// </summary>
    public List<TModel> ReadCsv(Stream csvStream)
    {
        csvStream.Position = 0;
        using var reader = new StreamReader(csvStream);
        return ReadCsvCore(reader, new ImportReadOptions(), null).Records;
    }

    /// <summary>
    ///     Read and import data from a CSV file. Throws on the first bad cell.
    /// </summary>
    public List<TModel> ReadCsv(string csvFilePath)
    {
        using var reader = new StreamReader(csvFilePath);
        return ReadCsvCore(reader, new ImportReadOptions(), null).Records;
    }

    /// <summary>
    ///     Read a CSV stream, collecting every cell failure instead of throwing on the first.
    /// </summary>
    public ImportResult<TModel> TryReadCsv(Stream csvStream, ImportReadOptions? options = null)
    {
        csvStream.Position = 0;
        using var reader = new StreamReader(csvStream);
        return ReadCsvCore(reader, options ?? new ImportReadOptions(), []);
    }

    /// <summary>
    ///     Read and import data from an Excel file. Throws on the first bad cell.
    /// </summary>
    public List<TModel> ReadXlsx(string xlsxFilePath)
    {
        using var stream = File.Open(xlsxFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return ReadXlsx(stream);
    }

    /// <summary>
    ///     Read and import data from an Excel stream, using the first worksheet. Throws on the first bad cell.
    /// </summary>
    public List<TModel> ReadXlsx(Stream stream)
    {
        return ReadXlsxCore(stream, new ImportReadOptions(), null).Records;
    }

    /// <summary>
    ///     Read and import data from a named worksheet. Prefer this over the positional overload whenever the workbook
    ///     may contain more than one sheet.
    /// </summary>
    public List<TModel> ReadXlsx(Stream stream, string sheetName)
    {
        return ReadXlsxCore(stream, new ImportReadOptions { SheetName = sheetName }, null).Records;
    }

    /// <summary>
    ///     Read an Excel stream, collecting every cell failure instead of throwing on the first.
    /// </summary>
    public ImportResult<TModel> TryReadXlsx(Stream stream, ImportReadOptions? options = null)
    {
        return ReadXlsxCore(stream, options ?? new ImportReadOptions(), []);
    }

    private ImportResult<TModel> ReadXlsxCore(Stream stream, ImportReadOptions options, List<ImportError>? errors)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = ResolveWorksheet(workbook, options.SheetName);

        var headerRow = worksheet.FirstRowUsed();
        var lastRow = worksheet.LastRowUsed();
        if (headerRow is null || lastRow is null)
        {
            throw new EmptyFileImportException("Imported file is empty");
        }

        var firstHeaderCell = headerRow.FirstCellUsed();
        var lastHeaderCell = headerRow.LastCellUsed();
        if (firstHeaderCell is null || lastHeaderCell is null)
        {
            throw new EmptyFileImportException("Imported file is empty");
        }

        var firstColumn = firstHeaderCell.Address.ColumnNumber;
        var lastColumn = lastHeaderCell.Address.ColumnNumber;

        var columnNumberByHeader = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var c = firstColumn; c <= lastColumn; c++)
        {
            // Read the header row positionally. Skipping blank cells here and then reading data by ordinal is what
            // silently shifted every column when the header row had a gap.
            var header = NormalizeHeader(headerRow.Cell(c).GetString());
            if (header is null)
            {
                continue;
            }

            if (!columnNumberByHeader.TryAdd(header, c))
            {
                throw new DuplicateColumnException(
                    $"Duplicate column '{header}' in the header row. Column names must be unique.", header);
            }
        }

        var plans = BuildPlans(columnNumberByHeader);
        var records = new List<TModel>();

        for (var r = headerRow.RowNumber() + 1; r <= lastRow.RowNumber(); r++)
        {
            var row = worksheet.Row(r);
            if (row.IsEmpty())
            {
                continue;
            }

            var record = BuildRecord(plans, index => ReadCell(row.Cell(index)), r, errors);
            if (record is not null)
            {
                records.Add(record);
            }

            if (errors is not null && errors.Count >= options.MaxErrors)
            {
                break;
            }
        }

        if (records.Count == 0 && (errors is null || errors.Count == 0))
        {
            throw new EmptyFileImportException("Imported file is empty");
        }

        return new ImportResult<TModel>(records, errors ?? []);
    }

    private ImportResult<TModel> ReadCsvCore(TextReader reader, ImportReadOptions options, List<ImportError>? errors)
    {
        using var csv = new CsvReader(reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                DetectDelimiter = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                BadDataFound = null
            });

        if (!csv.Read() || !csv.ReadHeader())
        {
            throw new EmptyFileImportException("Imported file is empty");
        }

        var headerRecord = csv.HeaderRecord ?? [];
        var columnNumberByHeader = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var i = 0; i < headerRecord.Length; i++)
        {
            var header = NormalizeHeader(headerRecord[i]);
            if (header is null)
            {
                continue;
            }

            if (!columnNumberByHeader.TryAdd(header, i + 1))
            {
                throw new DuplicateColumnException(
                    $"Duplicate column '{header}' in the header row. Column names must be unique.", header);
            }
        }

        var plans = BuildPlans(columnNumberByHeader);
        var records = new List<TModel>();
        var rowNumber = 1;

        while (csv.Read())
        {
            rowNumber++;

            if (IsBlankCsvRow(csv))
            {
                continue;
            }

            var record = BuildRecord(plans, index => ReadCsvField(csv, index), rowNumber, errors);
            if (record is not null)
            {
                records.Add(record);
            }

            if (errors is not null && errors.Count >= options.MaxErrors)
            {
                break;
            }
        }

        if (records.Count == 0 && (errors is null || errors.Count == 0))
        {
            throw new EmptyFileImportException("Imported file is empty");
        }

        return new ImportResult<TModel>(records, errors ?? []);
    }

    private static IXLWorksheet ResolveWorksheet(IXLWorkbook workbook, string? sheetName)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
        {
            return workbook.Worksheets.FirstOrDefault()
                   ?? throw new EmptyFileImportException("Imported file is empty");
        }

        foreach (var candidate in workbook.Worksheets)
        {
            if (string.Equals(candidate.Name.Trim(), sheetName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        throw new SheetNotFoundException($"Worksheet '{sheetName}' was not found in the workbook.", sheetName);
    }

    private List<ColumnPlan> BuildPlans(IReadOnlyDictionary<string, int> columnNumberByHeader)
    {
        var plans = new List<ColumnPlan>(_columns.Count);
        List<string>? missing = null;

        foreach (var column in _columns)
        {
            if (column.ValueSource != ImportValueSource.Column)
            {
                // Constant and computed columns are server-side; requiring a header for them was a defect.
                plans.Add(new ColumnPlan(column, null));
                continue;
            }

            int? cellIndex = null;

            foreach (var candidate in column.HeaderCandidates())
            {
                var normalized = NormalizeHeader(candidate);
                if (normalized is not null && columnNumberByHeader.TryGetValue(normalized, out var found))
                {
                    cellIndex = found;
                    break;
                }
            }

            if (cellIndex is null && column.IsRequired)
            {
                (missing ??= []).Add(column.ColumnName);
            }

            plans.Add(new ColumnPlan(column, cellIndex));
        }

        if (missing is null)
        {
            return plans;
        }

        var joined = string.Join(", ", missing);
        throw new MissingColumnsException(
            $"Required column(s) not found in the header row: {joined}.", joined)
        {
            MissingColumns = missing
        };
    }

    private TModel? BuildRecord(
        List<ColumnPlan> plans,
        Func<int, string?> readCell,
        int? rowNumber,
        List<ImportError>? errors)
    {
        var model = Activator.CreateInstance<TModel>();

        foreach (var plan in plans)
        {
            var column = plan.Column;
            string? raw = null;

            try
            {
                raw = plan.CellIndex is null ? null : readCell(plan.CellIndex.Value);
                column.Apply(model, column.Resolve(raw, model));
            }
            catch (Exception ex)
            {
                var error = BuildError(column, raw, rowNumber, ex);

                if (errors is null)
                {
                    throw new InvalidCellValueException(error.ToString(), column.ColumnName, error);
                }

                errors.Add(error);
                return null;
            }
        }

        return model;
    }

    private TModel? GetRecordByName(
        IReadOnlyDictionary<string, string> dataRow,
        int? rowNumber,
        List<ImportError>? errors)
    {
        var model = Activator.CreateInstance<TModel>();

        foreach (var column in _columns)
        {
            string? raw = null;

            try
            {
                if (column.ValueSource == ImportValueSource.Column)
                {
                    raw = FindByHeader(dataRow, column);
                }

                column.Apply(model, column.Resolve(raw, model));
            }
            catch (Exception ex)
            {
                var error = BuildError(column, raw, rowNumber, ex);

                if (errors is null)
                {
                    throw new InvalidCellValueException(error.ToString(), column.ColumnName, error);
                }

                errors.Add(error);
                return null;
            }
        }

        return model;
    }

    private static string? FindByHeader(IReadOnlyDictionary<string, string> row, IColumnBinding column)
    {
        foreach (var candidate in column.HeaderCandidates())
        {
            if (row.TryGetValue(candidate, out var direct))
            {
                return string.IsNullOrWhiteSpace(direct) ? null : direct;
            }

            var normalized = NormalizeHeader(candidate);
            if (normalized is not null && row.TryGetValue(normalized, out var value))
            {
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        return null;
    }

    private static ImportError BuildError(IColumnBinding column, string? raw, int? rowNumber, Exception ex)
    {
        return new ImportError(
            rowNumber,
            column.ColumnName,
            column.PropertyName,
            Truncate(raw, 256),
            column.Property.PropertyType.Name,
            GetInnermostMessage(ex));
    }

    private static string? ReadCell(IXLCell cell)
    {
        var value = cell.Value;

        if (value.IsBlank)
        {
            return null;
        }

        if (value.IsError)
        {
            return value.GetError()
                .ToString();
        }

        // Stringify invariantly. Relying on the culture-sensitive default while converting with InvariantCulture broke
        // every decimal and date column under a non-invariant server culture.
        if (value.IsNumber)
        {
            return value.GetNumber()
                .ToString(CultureInfo.InvariantCulture);
        }

        if (value.IsDateTime)
        {
            var dateTime = value.GetDateTime();
            return dateTime.TimeOfDay == TimeSpan.Zero
                ? dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        if (value.IsTimeSpan)
        {
            return value.GetTimeSpan()
                .ToString("c", CultureInfo.InvariantCulture);
        }

        if (value.IsBoolean)
        {
            return value.GetBoolean() ? "true" : "false";
        }

        var text = value.GetText();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string? ReadCsvField(IReaderRow csv, int index)
    {
        if (index > csv.Parser.Count)
        {
            return null;
        }

        var value = csv.GetField(index - 1);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool IsBlankCsvRow(IReaderRow csv)
    {
        for (var i = 0; i < csv.Parser.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(csv.GetField(i)))
            {
                return false;
            }
        }

        return true;
    }

    private static string? NormalizeHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Collapse every whitespace run to a single space and lowercase invariantly, so "Full  Name",
        // "Full Name" and "FULL NAME" all match a rule declaring "Full Name".
        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach (var ch in value)
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    private static string Truncate(string? value, int max)
    {
        if (value is null)
        {
            return "null";
        }

        return value.Length <= max ? value : string.Concat(value.AsSpan(0, max), "…");
    }

    private static string GetInnermostMessage(Exception ex)
    {
        while (ex.InnerException is not null)
        {
            ex = ex.InnerException;
        }

        return ex.Message;
    }

    private readonly record struct ColumnPlan(IColumnBinding Column, int? CellIndex);

    /// <summary>
    ///     Behaviour side of a column. Internal so the public contract stays pure metadata.
    /// </summary>
    internal interface IColumnBinding : IImportColumn
    {
        PropertyInfo Property { get; }

        IEnumerable<string> HeaderCandidates();

        object? Resolve(string? raw, TModel model);

        void Apply(TModel model, object? value);
    }

    /// <summary>
    ///     Property-level import rule configuration.
    /// </summary>
    public class PropertyRule<TProperty> : IColumnBinding
    {
        private readonly List<AllowedValue> _allowedValues = [];
        private readonly List<string> _aliases = [];
        private readonly PropertyInfo _property;

        private Func<TModel, TProperty>? _computeFromModel;
        private TProperty _constantValue = default!;

        private Func<string, TProperty> _converter =
            x => (TProperty)System.Convert.ChangeType(x, typeof(TProperty), CultureInfo.InvariantCulture);

        private ConverterType _converterType = ConverterType.None;

        private Func<string, TModel, TProperty> _converterWithInstance =
            (x, _) => (TProperty)System.Convert.ChangeType(x, typeof(TProperty), CultureInfo.InvariantCulture);

        private TProperty _defaultValue = default!;
        private string? _description;
        private Type? _enumSource;
        private string? _exampleText;
        private string? _expectedFormat;
        private bool _isValueRequired;
        private Regex? _regexCompiled;
        private string _regexPattern = ".*";

        internal PropertyRule(PropertyInfo property)
        {
            _property = property;
            PropertyName = property.Name;
            ColumnName = property.Name;
            PropertyType = Nullable.GetUnderlyingType(typeof(TProperty)) ?? typeof(TProperty);
            IsNullable = Nullable.GetUnderlyingType(typeof(TProperty)) is not null
                         || !typeof(TProperty).IsValueType;
        }

        /// <inheritdoc />
        public string PropertyName { get; }

        /// <inheritdoc />
        public string ColumnName { get; private set; }

        /// <inheritdoc />
        public IReadOnlyList<string> Aliases => _aliases;

        /// <inheritdoc />
        public Type PropertyType { get; }

        /// <inheritdoc />
        public bool IsNullable { get; }

        /// <inheritdoc />
        public bool IsRequired => _isValueRequired;

        /// <inheritdoc />
        public object? DefaultValue => _defaultValue;

        /// <inheritdoc />
        public string RegexPattern => _regexPattern;

        /// <inheritdoc />
        public bool HasCustomConverter => _converterType != ConverterType.None;

        /// <inheritdoc />
        public string? Description => _description;

        /// <inheritdoc />
        public string? ExpectedFormat => _expectedFormat;

        /// <inheritdoc />
        public object? Example { get; private set; }

        /// <inheritdoc />
        public string? ExampleText => _exampleText;

        /// <inheritdoc />
        public Type? EnumSource => _enumSource;

        /// <inheritdoc />
        public IReadOnlyList<AllowedValue> AllowedValues => _allowedValues;

        /// <inheritdoc />
        public ImportValueSource ValueSource { get; private set; } = ImportValueSource.Column;

        /// <inheritdoc />
        public bool HasValueSet => _enumSource is not null || _allowedValues.Count > 0;

        PropertyInfo IColumnBinding.Property => _property;

        IEnumerable<string> IColumnBinding.HeaderCandidates()
        {
            yield return ColumnName;

            foreach (var alias in _aliases)
            {
                yield return alias;
            }
        }

        object? IColumnBinding.Resolve(string? raw, TModel model)
        {
            return GetValue(raw, model);
        }

        void IColumnBinding.Apply(TModel model, object? value)
        {
            SetProperty(_property, model, value);
        }

        /// <summary>
        ///     Read from a specific column name (different from the property name).
        /// </summary>
        public PropertyRule<TProperty> ReadFromColumn(string name)
        {
            ColumnName = name;
            ValueSource = ImportValueSource.Column;
            return this;
        }

        /// <summary>
        ///     Also accept these header spellings. The canonical <c>ColumnName</c> is what generated templates emit.
        /// </summary>
        public PropertyRule<TProperty> WithAliases(params string[] aliases)
        {
            _aliases.AddRange(aliases.Where(a => !string.IsNullOrWhiteSpace(a)));
            return this;
        }

        /// <summary>
        ///     Validate the value against a regex pattern.
        /// </summary>
        public PropertyRule<TProperty> Validate(string regex)
        {
            _regexPattern = regex;
            _regexCompiled = CompileRegex(regex);
            return this;
        }

        /// <summary>
        ///     Use a custom converter function. Pair it with <see cref="WithExpectedFormat" /> whenever it expects a
        ///     specific textual shape, since the delegate itself cannot be inspected by the template generator.
        /// </summary>
        public PropertyRule<TProperty> Convert(Func<string, TProperty> func)
        {
            _converter = func;
            _converterType = ConverterType.Converter;
            return this;
        }

        /// <summary>
        ///     Use a custom converter function with access to the model instance.
        /// </summary>
        public PropertyRule<TProperty> Convert(Func<string, TModel, TProperty> func)
        {
            _converterWithInstance = func;
            _converterType = ConverterType.ConverterWithInstance;
            return this;
        }

        /// <summary>
        ///     Set a constant value for this property. Never read from the file, never templated.
        /// </summary>
        public PropertyRule<TProperty> WriteValue(TProperty value)
        {
            ValueSource = ImportValueSource.Constant;
            _constantValue = value;
            return this;
        }

        /// <summary>
        ///     Set a default value if the cell is null or empty.
        /// </summary>
        public PropertyRule<TProperty> Default(TProperty value)
        {
            _defaultValue = value;
            return this;
        }

        /// <summary>
        ///     Require that this property has a non-empty value. A required column must also be present in the file.
        /// </summary>
        public PropertyRule<TProperty> NotEmpty()
        {
            _isValueRequired = true;
            return this;
        }

        /// <summary>
        ///     Compute the value from the model instance. Never read from the file, never templated.
        /// </summary>
        public PropertyRule<TProperty> ReadFromModel(Func<TModel, TProperty> func)
        {
            ValueSource = ImportValueSource.Computed;
            _computeFromModel = func;
            return this;
        }

        /// <summary>
        ///     Human description of this column, rendered in the legend and as a header note.
        /// </summary>
        public PropertyRule<TProperty> Describe(string description)
        {
            _description = description;
            return this;
        }

        /// <summary>
        ///     Declare the textual format this column expects (e.g. <c>"dd/MM/yyyy"</c>). Generated templates render such
        ///     columns as text so Excel cannot coerce them.
        /// </summary>
        public PropertyRule<TProperty> WithExpectedFormat(string format)
        {
            _expectedFormat = format;
            return this;
        }

        /// <summary>
        ///     Example cell value used in generated sample rows.
        /// </summary>
        public PropertyRule<TProperty> WithExample(TProperty value)
        {
            Example = value;
            return this;
        }

        /// <summary>
        ///     Literal text to write into this column's sample cells, used verbatim. Declare it instead of
        ///     <see cref="WithExample" /> when the destination type cannot be rendered as the text the parser expects:
        ///     an example typed to a collection is written as its type name, which the rule then rejects. It wins over
        ///     <see cref="WithExample" /> and over the value read from a supplied example model, so a column that
        ///     declares it shows the same text on every sample row.
        /// </summary>
        public PropertyRule<TProperty> WithExampleText(string text)
        {
            _exampleText = text;
            return this;
        }

        /// <summary>
        ///     Declare that this column carries values of <typeparamref name="TEnum" />. Required on <c>int</c> columns:
        ///     the property type alone cannot reveal which enum it represents.
        /// </summary>
        public PropertyRule<TProperty> WithEnumSource<TEnum>() where TEnum : struct, Enum
        {
            _enumSource = typeof(TEnum);
            return this;
        }

        /// <summary>
        ///     Declare a closed value set for a column that is not backed by an enum.
        /// </summary>
        public PropertyRule<TProperty> WithAllowedValues(params AllowedValue[] values)
        {
            _allowedValues.AddRange(values);
            return this;
        }

        /// <summary>
        ///     Get the converted value for this property.
        /// </summary>
        public TProperty GetValue(string? value, TModel model)
        {
            switch (ValueSource)
            {
                case ImportValueSource.Constant:
                    return _constantValue;
                case ImportValueSource.Computed:
                    return _computeFromModel is null ? _defaultValue : _computeFromModel(model) ?? _defaultValue;
            }

            // A generated dropdown writes "0 - Label"; strip it back to the code before anything else looks at it,
            // so a Validate pattern like ^(0|1|2)$ still applies to what the importer actually consumes.
            var normalized = NormalizeCodedValue(value);

            if (_isValueRequired && string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidColumnValueException("Column value is required", $"{ColumnName}: {value}");
            }

            var innerValue = normalized ?? _defaultValue?.ToString();
            if (innerValue is null)
            {
                return _defaultValue;
            }

            _regexCompiled ??= CompileRegex(_regexPattern);
            if (!_regexCompiled.IsMatch(innerValue))
            {
                throw new InvalidColumnValueException("Column value is not valid", $"{ColumnName}: {value}");
            }

            return _converterType switch
            {
                ConverterType.Converter => _converter(innerValue) ?? _defaultValue,
                ConverterType.ConverterWithInstance => _converterWithInstance(innerValue, model) ?? _defaultValue,
                _ => ChangeType(innerValue, PropertyType) ?? _defaultValue
            };
        }

        private string? NormalizeCodedValue(string? raw)
        {
            if (raw is null || !HasValueSet)
            {
                return raw;
            }

            var separator = raw.IndexOf(ImportTemplateSeparators.CodeLabel, StringComparison.Ordinal);
            if (separator > 0)
            {
                return raw[..separator]
                    .Trim();
            }

            if (_allowedValues.Count == 0)
            {
                return raw;
            }

            var trimmed = raw.Trim();
            foreach (var allowed in _allowedValues)
            {
                if (string.Equals(allowed.Label, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return allowed.Value;
                }
            }

            return raw;
        }

        private static Regex CompileRegex(string pattern)
        {
            return new Regex(pattern,
                RegexOptions.ExplicitCapture | RegexOptions.Compiled,
                TimeSpan.FromMilliseconds(250));
        }

        private static void SetProperty(PropertyInfo property, object target, object? value)
        {
            if (property.SetMethod is not null && !IsInitOnly(property))
            {
                property.SetValue(target, value);
                return;
            }

            var backingField = property.DeclaringType!.GetField($"<{property.Name}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (backingField is null)
            {
                throw new InvalidColumnValueException($"Property '{property.Name}' is not settable.", property.Name);
            }

            backingField.SetValue(target, value);
        }

        private static bool IsInitOnly(PropertyInfo property)
        {
            var setter = property.SetMethod;
            if (setter is null)
            {
                return false;
            }

            return setter.ReturnParameter.GetRequiredCustomModifiers()
                .Any(static m => m == typeof(IsExternalInit));
        }

        private static TProperty? ChangeType(string innerValue, Type type)
        {
            if (type.IsEnum)
            {
                var parsed = Enum.Parse(type, innerValue, true);

                // Enum.Parse happily accepts any numeric string, so "99" would silently become an undefined member.
                // [Flags] enums legitimately combine values, so they are exempt.
                if (!type.IsDefined(typeof(FlagsAttribute), false) && !Enum.IsDefined(type, parsed))
                {
                    throw new InvalidColumnValueException(
                        $"Value '{innerValue}' is not a valid {type.Name}", innerValue);
                }

                return (TProperty?)parsed;
            }

            if (type != typeof(bool))
            {
                return (TProperty?)System.Convert.ChangeType(innerValue, type, CultureInfo.InvariantCulture);
            }

            if (bool.TryParse(innerValue, out var parsedBool))
            {
                return (TProperty?)(object)parsedBool;
            }

            if (int.TryParse(innerValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
            {
                return (TProperty?)(object)(parsedInt != 0);
            }

            if (string.Equals(innerValue, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return (TProperty?)(object)true;
            }

            if (string.Equals(innerValue, "no", StringComparison.OrdinalIgnoreCase))
            {
                return (TProperty?)(object)false;
            }

            return (TProperty?)System.Convert.ChangeType(innerValue, type, CultureInfo.InvariantCulture);
        }
    }
}
