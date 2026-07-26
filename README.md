# Pandatech.FluentImporter

Fluent API for importing CSV and Excel data into .NET 8+ applications with customizable property mapping and validation.

## Installation

```bash
dotnet add package PandaTech.FluentImporter
```

## Quick Start

### Define Your Model

```csharp
public class FileData
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; }
}
```

### Create Import Rules

```csharp
public class FileDataImportRule : ImportRule<FileData>
{
    public FileDataImportRule()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Description)
            .ReadFromColumn("Description text")
            .Default("No Description");

        RuleFor(x => x.Date)
            .ReadFromColumn("Date")
            .Convert(DateTime.Parse);

        RuleFor(x => x.Comment)
            .ReadFromColumn("Comment");

        RuleFor(x => x.Id)
            .ReadFromColumn("Id")
            .Convert(s => long.Parse(s));

        RuleFor(x => x.CreatedAt)
            .WriteValue(DateTime.UtcNow);

        RuleFor(x => x.CreatedBy)
            .ReadFromModel(x => x.CreatedBy + " - Modified");
    }
}
```

### Import Data

```csharp
var importRule = new FileDataImportRule();

// From CSV file
var data = importRule.ReadCsv("path/to/file.csv");

// From CSV stream
using var stream = File.OpenRead("file.csv");
var data = importRule.ReadCsv(stream);

// From Excel file
var data = importRule.ReadXlsx("path/to/file.xlsx");

// From Excel stream
using var stream = File.OpenRead("file.xlsx");
var data = importRule.ReadXlsx(stream);

// From in-memory data
var dict = new List<Dictionary<string, string>>
{
    new() { ["Name"] = "John", ["Date"] = "2024-01-01" }
};
var data = importRule.GetRecords(dict);
```

### Read with error aggregation

`ReadCsv` / `ReadXlsx` throw on the first bad cell. To report every bad cell at once, use the `Try` variants:

```csharp
var result = importRule.TryReadXlsx(stream, new ImportReadOptions { SheetName = "Data", MaxErrors = 200 });

foreach (var error in result.Errors)
{
    // error.RowNumber, error.ColumnName, error.PropertyName, error.RawValue, error.Message
}

return result.HasErrors ? BadRequest(result.Errors) : Save(result.Records);
```

Pass `SheetName` whenever the workbook may contain more than one sheet. Without it the first sheet by tab position
is used, which means a legend or instruction sheet placed first would be read as the data sheet.

## Generating the template

An import rule already knows every column, so it can emit the file the operator fills in. Generating the template
from the rule is the only way to guarantee the download and the parser cannot drift apart.

The result has two sheets: the **fillable data sheet first** (so it is the tab Excel opens on) and a **legend
second**. Enum columns get an in-cell dropdown sourced from the legend, and the dropdown inserts `"0 - Label"`
which the reader normalizes back to `0` — so the operator picks something meaningful while the column still
imports as an integer.

```csharp
public class ProductImportRule : ImportRule<ProductImportModel>
{
    public ProductImportRule()
    {
        RuleFor(x => x.CategoryId).ReadFromColumn("Category Id").NotEmpty()
            .Describe("Id of the category this product belongs to").WithExample(1);

        RuleFor(x => x.Status).ReadFromColumn("Status").NotEmpty()
            .WithEnumSource<ProductStatus>()        // an int column that carries an enum
            .Describe("Product status").WithExample(0);

        RuleFor(x => x.ReleasedOn).ReadFromColumn("Released On")
            .Convert(ParseDayFirst)
            .WithExpectedFormat("dd/MM/yyyy");      // a converter is opaque; declare what it expects
    }
}

var bytes = new ProductImportRule().BuildTemplate(options: new ImportTemplateOptions
{
    DataSheetName = "Products",
    LegendSheetName = "Legend",
    EnumLabelResolver = (enumType, member, value) => localizer.Label(enumType, member),
    ColumnDescriptionResolver = column => localizer.ColumnDescription(column.ColumnName),
    Texts = new ImportTemplateTexts { LegendTitle = "Legend", RequiredYes = "Yes", /* … */ }
});
```

The library holds no opinion about language: sheet names, captions, column descriptions and enum labels are all
whatever `EnumLabelResolver`, `ColumnDescriptionResolver` and `Texts` return, in any script. The data sheet's
header row is the one thing never localized — it is the parsing contract, so it is always the rule's canonical
`ReadFromColumn` text.

## Inspecting the columns

`ImportRule<T>.Columns` exposes every configured column in declaration order as `IImportColumn`:
`ColumnName`, `Aliases`, `PropertyName`, `PropertyType`, `IsRequired`, `IsNullable`, `DefaultValue`,
`RegexPattern`, `HasCustomConverter`, `Description`, `ExpectedFormat`, `Example`, `EnumSource`,
`AllowedValues`, `ValueSource`, `HasValueSet`.

## API Reference

### Property Rules

| Method                             | Description                                                          |
|------------------------------------|----------------------------------------------------------------------|
| `ReadFromColumn(string)`           | Map to a different column name                                       |
| `WithAliases(params string[])`      | Also accept these header spellings                                   |
| `NotEmpty()`                       | Require a non-empty value; the column must also be present           |
| `Default(T)`                       | Set default value if null/empty                                      |
| `Convert(Func<string, T>)`         | Custom converter function                                            |
| `Convert(Func<string, TModel, T>)` | Converter with access to model instance                              |
| `Validate(string)`                 | Regex validation pattern                                             |
| `WriteValue(T)`                    | Set a constant value; never read from the file, never templated      |
| `ReadFromModel(Func<TModel, T>)`   | Compute value from model; never read from the file, never templated  |
| `Describe(string)`                 | Legend description and header note                                   |
| `WithExample(T)`                   | Example value for generated sample rows                              |
| `WithExpectedFormat(string)`       | Textual format a custom converter expects, e.g. `dd/MM/yyyy`         |
| `WithEnumSource<TEnum>()`          | Declare the enum an `int` column carries; drives legend and dropdown |
| `WithAllowedValues(...)`           | Declare a closed value set for a non-enum column                     |

### Header matching

Case-insensitive, outer whitespace trimmed, and inner whitespace runs collapsed — `"Full Name"`,
`"FULL NAME"`, `"  full   name  "` and `"Full Name"` all match a rule declaring `Full Name`. Everything
else is exact: spelling, hyphens, and Armenian vs Latin script are load-bearing. Extra unmapped columns are
ignored; an optional mapped column may be absent entirely.

### Supported Types

- Primitives: `string`, `int`, `long`, `decimal`, `double`, `bool`, etc.
- `DateTime`, `DateTimeOffset`, `Guid`
- `Nullable<T>` for all value types
- `Enum` types (case-insensitive)
- Custom types via `Convert()` method

### Boolean Parsing

The library automatically handles multiple boolean representations:

- `true`/`false`
- `1`/`0`
- `yes`/`no`

## Error Handling

All import exceptions inherit from `ImportException`:

| Exception                       | Raised when                                                              |
|---------------------------------|--------------------------------------------------------------------------|
| `EmptyFileImportException`      | File contains no data rows                                               |
| `MissingColumnsException`       | Required columns are absent from the header row (checked before any row)  |
| `DuplicateColumnException`      | Two header cells normalize to the same column name                       |
| `SheetNotFoundException`        | A worksheet was requested by name and does not exist                     |
| `InvalidCellValueException`     | A cell value failed validation or conversion                             |
| `InvalidColumnValueException`   | A value did not satisfy the rule                                         |
| `InvalidPropertyNameException`  | A rule references a property that does not exist on the model            |

`InvalidCellValueException.Error` carries the failure as structured data (`ImportError`): row number, column name,
property name, raw value, target type and message. The `Try` read variants return these instead of throwing.

## License

MIT
