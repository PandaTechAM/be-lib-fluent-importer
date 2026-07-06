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

## API Reference

### Property Rules

| Method                             | Description                             |
|------------------------------------|-----------------------------------------|
| `ReadFromColumn(string)`           | Map to a different column name          |
| `NotEmpty()`                       | Require non-empty value                 |
| `Default(T)`                       | Set default value if null/empty         |
| `Convert(Func<string, T>)`         | Custom converter function               |
| `Convert(Func<string, TModel, T>)` | Converter with access to model instance |
| `Validate(string)`                 | Regex validation pattern                |
| `WriteValue(T)`                    | Set a constant value                    |
| `ReadFromModel(Func<TModel, T>)`   | Compute value from model                |

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

- `EmptyFileImportException` - File contains no data rows
- `InvalidCellValueException` - Cell value failed validation or conversion
- `InvalidColumnValueException` - Required column not found
- `InvalidPropertyNameException` - Property doesn't exist on model

Each exception includes:

- Descriptive error message
- Row number (when applicable)
- Column name
- Property name
- Raw value that failed

## License

MIT
