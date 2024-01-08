using Microsoft.EntityFrameworkCore;

namespace FileImporter.Tests;

public class ServiceTests
{
    private List<Dictionary<string, string>> data = new()
    {
        new Dictionary<string, string>()
        {
            { "Id", "1" },
            { "Name", "Name1" },
            { "Description", "Description1" },
            { "Date", "2021-01-01" },
            { "Comment", "Comment1" }
        },
        new Dictionary<string, string>()
        {
            { "Id", "3" },
            { "Name", "Name1" },
            { "Description", "Description1" },
            { "Date", "2021-01-01" },
            { "Comment", "Comment1" }
        },
        new Dictionary<string, string>()
        {
            { "Id", "4" },
            { "Name", "Name1" },
            { "Description", "Description1" },
            { "Date", "2021-01-01" },
            { "Comment", "Comment1" }
        }
    };

    private readonly MyContext _context;

    public ServiceTests()
    {
        var options = new DbContextOptionsBuilder<MyContext>()
            .UseInMemoryDatabase(databaseName: "MyContext")
            .Options;

        _context = new MyContext(options);
        _context.Database.EnsureCreated();
    }


    [Fact]
    public async Task Import_Data()
    {
        var rule = new FileDataImportRule();
        await rule.ImportAsync(_context, data);

        Assert.Equal(3, _context.Data.Count());
    }
}