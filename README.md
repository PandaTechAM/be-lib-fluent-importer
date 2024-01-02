# PandaTech.FileImporter Library

This library provides a robust and flexible way to import data from Excel files into a database. It is built in C# and uses the Entity Framework for database operations.

## Features

- Import data from Excel files into any Entity Framework supported database.
- Define custom import rules for each property of your model.
- Supports data conversion during the import process.
- Asynchronous import operations for better performance.

## Getting Started

### Prerequisites

- .NET 8.0 or higher
- Entity Framework Core 8.0 or higher

### Installation

Add a reference to the `PandaTechFileImporter` project in your solution.

### Usage

1. Define a model for your data, that you want to import.

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

2. Define an import rule for your model.

```csharp
public class FileDataImportRule: ImportRule<FileData>
{
    public FileDataImportRule()
    {
        RuleFor(x => x.Name);
        RuleFor(x => x.Description).ReadFromColumn("Description text");
        RuleFor(x => x.Date).ReadFromColumn("Date").Convert(DateTime.Parse);
        RuleFor(x => x.Comment).ReadFromColumn("Comment");
        RuleFor(x => x.Id).ReadFromColumn("Id").Convert(s => BaseConverter.PandaBaseConverter.Base36ToBase10(s)!.Value);
        RuleFor(x => x.CreatedAt).WriteValue(DateTime.UtcNow);
        RuleFor(x => x.CreatedBy).WriteValue("System");
        RuleFor(x => x.CreatedBy).ReadFromModel(x => x.CreatedBy + " - 1");
    }
}
```

 - In RuleFor, you can specify the property of your model, that you want to import.
 - Use the `ReadFromColumn` method to specify the column name in the Excel file, that contains the data for the property.
 - Use the `WriteValue` method to specify a constant value for the property.
 - Use the `ReadFromModel` method to specify a function, that will be called to get the value for the property.The function takes the model as an argument.
> If you do not specify any `ReadFrom` method, the library will try to find a column with the same name as the property in the data.
 - Use the `Convert` method to specify a conversion function for the data. This function will be applied to the data before it is assigned to the property. 
The `Convert` method takes a `Func<string, T>` as an argument, where `T` is the type of the property.
It also has a generic overload, that takes a `Func<string, TModel, T>` as an argument, where `TModel` is the type of the model and `T` is the type of the property. 
> You can use `RuleFor` multiple times to specify multiple rules for the same property. The rules will be applied in the order they are specified.

3. Use one of the `Import` methods to import the data.

3.1. Import from memory.

```csharp
    var data = new List<Dictionary<string, string>>()
    // fill data here ...

    var rule = new FileDataImportRule();
    await rule.ImportAsync(_context, data);
```

`ImportAsync` takes a list of dictionaries as an argument. Each dictionary represents a row in the imported data. The keys of the dictionary are the column names and the values are the data in the columns.

3.2. Import from an `Excel` file.

```csharp
    var rule = new FileDataImportRule2();

    byte[] bytes;
    using (var memoryStream = new MemoryStream())
    {
        await file.CopyToAsync(memoryStream);
        bytes = memoryStream.ToArray();
    }

    await rule.ImportFromExcelAsync(_context, bytes);
```

`ImportFromExcelAsync` takes a byte array as an argument. The byte array should contain the data of the Excel file.

The excel file should have a header row, that contains the column names.

3.3. Import from a `CSV` file.

```csharp
    var rule = new FileDataImportRule();
    await rule.ImportFromCsvAsync(_context, csvText);
    return Ok(_context.Data.ToListAsync());
```

`ImportFromCsvAsync` takes a string as an argument. The string should contain the data of the CSV file.


## Contributing

Contributions are welcome. Please open an issue or submit a pull request on GitHub.

## License

This project is licensed under the MIT License.
