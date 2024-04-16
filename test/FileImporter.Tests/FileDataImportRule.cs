using FileImporter;

namespace FileImporter.Tests;

public class FileDataImportRule : ImportRule<FileData>
{
    public FileDataImportRule()
    {
        RuleFor(x => x.Name).ReadFromColumn("Name");
        RuleFor(x => x.Description).ReadFromColumn("Description");
        RuleFor(x => x.Date).ReadFromColumn("Date").Convert(DateTime.Parse);
        RuleFor(x => x.Comment).ReadFromColumn("Comment");
        RuleFor(x => x.Id).ReadFromColumn("Id").Convert(long.Parse);
    }
}