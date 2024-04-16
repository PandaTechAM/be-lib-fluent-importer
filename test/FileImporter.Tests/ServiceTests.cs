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

    public ServiceTests()
    {
    }


    [Fact]
    public async Task Import_Data()
    {
        var rule = new FileDataImportRule();
        var records = rule.GetRecords(data);

        Assert.Equal(3, records.Count());
    }
}