using FileImporter.Demo.Models;

namespace FileImporter.Demo;

public class FileDataImportRule2: ImportRule<FileData>
{
    public FileDataImportRule2()
    {
        RuleFor(x => x.Name).ReadFromColumn("Name");
        RuleFor(x => x.Description).ReadFromColumn("description details");
        RuleFor(x => x.Date).ReadFromColumn("date").Custom(DateTime.Parse);
        RuleFor(x => x.Comment).ReadFromColumn("comment");
        RuleFor(x => x.Id).ReadFromColumn("Id").Custom(s => BaseConverter.PandaBaseConverter.Base36ToBase10(s)!.Value);
    }
}