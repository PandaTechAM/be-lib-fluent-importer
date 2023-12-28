﻿using Pandatech.FileImporter;

namespace Pandatech.TestApi;

public class FileDataImportRule: ImportRule<FileData>
{
    public FileDataImportRule()
    {
        RuleFor(x => x.Name).ReadFromColumn("Name");
        RuleFor(x => x.Description).ReadFromColumn("Description");
        RuleFor(x => x.Date).ReadFromColumn("Date").Convert(DateTime.Parse);
        RuleFor(x => x.Comment).ReadFromColumn("Comment");
        RuleFor(x => x.Id).ReadFromColumn("Id").Convert(s => BaseConverter.PandaBaseConverter.Base36ToBase10(s)!.Value);
        RuleFor(x => x.CreatedAt).ReadFromValue(DateTime.UtcNow);
        RuleFor(x => x.CreatedBy).ReadFromValue("System");
        RuleFor(x => x.CreatedBy).ReadFromModel(x => x.CreatedBy + " - 1");
    }
}