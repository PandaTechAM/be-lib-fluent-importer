using Pandatech.FileImporter;

namespace Pandatech.TestApi;

public class FileDataImportRule2: ImportRule<FileData>
{
    public FileDataImportRule2()
    {
        RuleFor(x => x.Name).ReadFromColumn("Name");
        RuleFor(x => x.Description).ReadFromColumn("description details");
        RuleFor(x => x.Date).ReadFromColumn("date").Convert(DateTime.Parse);
        RuleFor(x => x.Comment).ReadFromColumn("comment");
        RuleFor(x => x.Id).ReadFromColumn("Id").Convert(s => BaseConverter.PandaBaseConverter.Base36ToBase10(s)!.Value);
    }
}