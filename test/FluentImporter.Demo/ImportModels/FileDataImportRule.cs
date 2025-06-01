using FluentImporter.Demo.Models;

namespace FluentImporter.Demo.ImportModels;

public class FileDataImportRule : ImportRule<FileData>
{
    public FileDataImportRule()
    {
        RuleFor(x => x.Name).ReadFromColumn("Name").Default("John");
        RuleFor(x => x.Description).ReadFromColumn("Description"); 
        RuleFor(x => x.Date).ReadFromColumn("Date").Convert(DateTime.Parse).Default(DateTime.UtcNow);
        RuleFor(x => x.Comment).ReadFromColumn("Comment").NotEmpty();
        RuleFor(x => x.NullableId).ReadFromColumn("Nullable Id").Default(1000);
        RuleFor(x => x.CreatedAt).WriteValue(DateTime.UtcNow);
        RuleFor(x => x.CreatedBy).WriteValue("System");
        RuleFor(x => x.Sqm).ReadFromColumn("Sqm").Convert(x => (int)(decimal.Parse(x ?? "0") * 1000) / 1000M);
    }
}