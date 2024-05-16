using FluentImporter.Demo.Models;

namespace FluentImporter.Demo.ImportModels;

public class FileDataImportRule2: ImportRule<FileData>
{
    public FileDataImportRule2()
    {
        // RuleFor(x => x.Name).ReadFromColumn("name");
        // RuleFor(x => x.Description).ReadFromColumn("description details");
        // RuleFor(x => x.Date).ReadFromColumn("date").Convert(DateTime.Parse);
        // RuleFor(x => x.Comment).ReadFromColumn("comment");
        // RuleFor(x => x.IsEnabled).ReadFromColumn("is enabled");
        // RuleFor(x => x.CreatedAt).ReadFromColumn("created at").Default(DateTime.UtcNow);
        // RuleFor(x => x.Id).ReadFromColumn("id").Convert(s => BaseConverter.PandaBaseConverter.Base36ToBase10(s)!.Value);
        
        RuleFor(x => x.Name).ReadFromColumn("Name");
        RuleFor(x => x.Description).ReadFromColumn("Description Details");
        RuleFor(x => x.Date).ReadFromColumn("Date").Convert(DateTime.Parse);
        RuleFor(x => x.Comment).ReadFromColumn("Comment");
        RuleFor(x => x.IsEnabled).ReadFromColumn("Is Enabled");
        RuleFor(x => x.CreatedAt).ReadFromColumn("Created At").Default(DateTime.UtcNow);
        RuleFor(x => x.Id).ReadFromColumn("Id").Convert(s => BaseConverter.PandaBaseConverter.Base36ToBase10(s)!.Value);
    }
}