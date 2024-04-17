# Pandatech FluentImporter

The `Pandatech.FluentImporter` is a lightweight NuGet package designed to facilitate the importation of CSV and Excel data
into .NET 8 or higher applications. Featuring a fluent API, the FluentImporter enables developers to specify custom import
rules for each model property, making the data import process both straightforward and flexible.

## Features

- Effortlessly import CSV and XLSX (excel) data.
- Define custom import rules using a fluent API.

## Installation

You can install Pandatech FileImporter via NuGet Package Manager or .NET CLI:

```bash
dotnet add package PandaTech.FluentImporter
```

## Usage

### Define Model

First, define a model class with properties that represent the data fields you wish to import. For example:

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

Next, create a class for import rules by inheriting from `ImportRule<TModel>` and define your rules using the fluent API. For example:

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

Finally, use the methods provided in your import rule instance to import data from CSV, Excel, or in-memory sources.

Import from In-Memory Data
```csharp
IEnumerable<TModel> GetRecords(IEnumerable<Dictionary<string, string>> data)
```
Import from CSV

```csharp
List<TModel> ReadCsv(Stream csvStream)
List<TModel> ReadCsv(string csvFilePath)
```
Import from Excel
```csharp
List<TModel> ReadXlsx(Stream stream)
List<TModel> ReadXlsx(string csvFilePath)
```
### Example

```csharp
var importRule = new FileDataImportRule();
var csvFilePath = "path/to/your/file.csv";
var importedData = importRule.ReadCsv(csvFilePath);
```

## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

## License

This project is licensed under the MIT License.
