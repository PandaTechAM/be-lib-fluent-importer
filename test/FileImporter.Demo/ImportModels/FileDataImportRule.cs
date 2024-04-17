using FileImporter.Demo.Models;

namespace FileImporter.Demo.ImportModels;

public class FileDataImportRule : ImportRule<FileData>
{
    public FileDataImportRule()
    {
        RuleFor(x => x.Name).ReadFromColumn("Name").Default("John");
        RuleFor(x => x.Description).ReadFromColumn("Description"); 
        RuleFor(x => x.Date).ReadFromColumn("Date").Convert(DateTime.Parse).Default(DateTime.UtcNow);
        RuleFor(x => x.Comment).ReadFromColumn("Comment").NotEmpty();
        RuleFor(x => x.Id).ReadFromColumn("Id").Convert(s => BaseConverter.PandaBaseConverter.Base36ToBase10(s)!.Value);
        RuleFor(x => x.CreatedAt).WriteValue(DateTime.UtcNow);
        RuleFor(x => x.CreatedBy).WriteValue("System");
    }
}