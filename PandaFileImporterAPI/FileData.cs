using PandaFileImporter;

namespace PandaFileImporterAPI;

public class FileData
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Comment { get; set; }
}

public class FileDataImportRule: ImportRule<FileData>
{
    public FileDataImportRule()
    {
        RuleFor(x => x.Name).ReadFromColumn("Name");
        RuleFor(x => x.Description).ReadFromColumn("Description");
        RuleFor(x => x.Date).ReadFromColumn("Date").Convert(DateTime.Parse);
        RuleFor(x => x.Comment).ReadFromColumn("Comment");
        RuleFor(x => x.Id).ReadFromColumn("Id").Convert(s => BaseConverter.PandaBaseConverter.Base36ToBase10(s)!.Value);
    }
}

public class FileDataImportRule2: ImportRule<FileData>
{
    public FileDataImportRule2()
    {
        RuleFor(x => x.Name).ReadFromColumn("Name");
        RuleFor(x => x.Description).ReadFromColumn("ddescription");
        RuleFor(x => x.Date).ReadFromColumn("date").Convert(DateTime.Parse);
        RuleFor(x => x.Comment).ReadFromColumn("comment");
        RuleFor(x => x.Id).ReadFromColumn("Id").Convert(s => BaseConverter.PandaBaseConverter.Base36ToBase10(s)!.Value);
    }
}