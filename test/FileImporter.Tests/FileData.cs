namespace FileImporter.Tests;

public class FileData
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Comment { get; set; }
}