using Microsoft.EntityFrameworkCore;

namespace FluentImporter.Tests;

public class ServiceTests
{
    private List<Dictionary<string, string>> data = new()
    {
        new Dictionary<string, string>()
        {
            { "Id".ToLower(), "1" },
            { "Name".ToLower(), "Name1" },
            { "Description".ToLower(), "Description1" },
            { "Date".ToLower(), "2021-01-01" },
            { "Comment".ToLower(), "Comment1" }
        },
        new Dictionary<string, string>()
        {
            { "Id".ToLower(), "3" },
            { "Name".ToLower(), "Name1" },
            { "Description".ToLower(), "Description1" },
            { "Date".ToLower(), "2021-01-01" },
            { "Comment".ToLower(), "Comment1" }
        },
        new Dictionary<string, string>()
        {
            { "Id".ToLower(), "4" },
            { "Name".ToLower(), "Name1" },
            { "Description".ToLower(), "Description1" },
            { "Date".ToLower(), "2021-01-01" },
            { "Comment".ToLower(), "Comment1" }
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