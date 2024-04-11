using FileImporter.Demo.Models;

namespace FileImporter.Demo.ImportModels;

public class FileDataImportRule : ImportRule<FileData>
{
    public FileDataImportRule()
    {
        RuleFor(x => x.Name).ReadFromColumn("Name"); //.NotEmpty();
        RuleFor(x => x.Description).ReadFromColumn("Description"); //.Default("No description");
        RuleFor(x => x.Date).ReadFromColumn("Date").Custom(DateTime.Parse);
        RuleFor(x => x.Comment).ReadFromColumn("Comment");
        RuleFor(x => x.Id).ReadFromColumn("Id").Custom(s => BaseConverter.PandaBaseConverter.Base36ToBase10(s)!.Value);
        RuleFor(x => x.CreatedAt).WriteValue(DateTime.UtcNow);
        RuleFor(x => x.CreatedBy).WriteValue("System");
    }
}