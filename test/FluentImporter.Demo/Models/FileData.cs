﻿using Microsoft.EntityFrameworkCore;

namespace FluentImporter.Demo.Models;

[PrimaryKey(nameof(Id))]
public class FileData
{
    public long Id { get; set; }
    public long? NullableId { get; set; }
    public decimal? Sqm { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Comment { get; set; }
    public bool? IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;
}