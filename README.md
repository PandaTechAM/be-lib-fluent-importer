# Pandatech FileImporter NuGet Package

Pandatech FileImporter is a lightweight NuGet package designed to simplify the process of importing CSV and Excel data into .NET 8 or higher applications. With a fluent API, Pandatech FileImporter  allows developers to define custom import rules for each property of their model, making data importing straightforward and flexible.

## Features

- Import CSV and Excel data effortlessly.
- Define custom import rules using a fluent API.
- Compatible with .NET 8 or higher versions.

## Installation

You can install Pandatech FileImporter via NuGet Package Manager or .NET CLI:

```bash
dotnet add package Pandatech FileImporter 
```

## Usage

### Define Model

First, define your model class with properties representing the data fields you want to import. For example:

```csharp
public class FileData
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
}
```

### Define Import Rules

Next, create an import rule class by inheriting from `ImportRule<TModel>` and define your import rules using the fluent API. For example:

```csharp
public class FileDataImportRule : ImportRule<FileData>
{
    public FileDataImportRule()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Description).ReadFromColumn("Description text").Default("No Description");
        RuleFor(x => x.Date).ReadFromColumn("Date").Convert(DateTime.Parse);
        RuleFor(x => x.Comment).ReadFromColumn("Comment");
        RuleFor(x => x.Id).ReadFromColumn("Id").Convert(s => BaseConverter.PandaBaseConverter.Base36ToBase10(s)!.Value);
        RuleFor(x => x.CreatedAt).WriteValue(DateTime.UtcNow);
        RuleFor(x => x.CreatedBy).WriteValue("System");
        RuleFor(x => x.CreatedBy).ReadFromModel(x => x.CreatedBy + " - 1");
    }
}
```

### Import Data

Finally, use one of the provided methods on the import rule instance to import data from CSV, Excel, or in-memory sources.

#### Import from In-Memory Data

```csharp
IEnumerable<TModel> GetRecords(IEnumerable<Dictionary<string, string>> data)
```

#### Import from CSV

```csharp
List<TModel> GetCsvRecords(Stream csvStream)
List<TModel> GetCsvRecords(string csvFilePath)
```

#### Import from Excel

```csharp
List<TModel> GetExcelRecords(Stream stream)
```

## Example

```csharp
var importRule = new FileDataImportRule();
var csvFilePath = "path/to/your/file.csv";
var importedData = importRule.GetCsvRecords(csvFilePath);
```

## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

## License

This project is licensed under the [MIT License](LICENSE).