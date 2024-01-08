using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FileImporter.Demo.Controllers;

[ApiController]
[Route("api/v1")]
public class ImportController : ControllerBase
{
    private readonly MyContext _context;

    public ImportController()
    {
        var options = new DbContextOptionsBuilder<MyContext>()
            .UseInMemoryDatabase(databaseName: "MyContext")
            .Options;

        _context = new MyContext(options);
        _context.Database.EnsureCreated();
    }

    [HttpPost("import-file-data-dictionary")]
    public async Task<IActionResult> ImportFileData()
    {
        var data = new List<Dictionary<string, string>>
        {
            new()
            {
                { "Id", "1" },
                { "Name", "Name1" },
                { "Description", "Description1" },
                { "Date", "2021-01-01" },
                { "Comment", "Comment1" }
            },
            new()
            {
                { "Id", "3" },
                { "Name", "Name1" },
                { "Description", "Description1" },
                { "Date", "2021-01-01" },
                { "Comment", "Comment1" }
            },
            new()
            {
                { "Id", "d4" },
                { "Name", "Name1" },
                { "Description", "Description1" },
                { "Date", "2021-01-01" },
                { "Comment", "Comment1" }
            }
        };

        var rule = new FileDataImportRule();
        await rule.ImportAsync(_context, data);
        return Ok(_context.Data.ToListAsync());
    }

    [HttpPost("import-file-data-xlsx")]
    public async Task<IActionResult> GetFileBytes(IFormFile file)
    {
        var rule = new FileDataImportRule2();

        byte[] bytes;
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            bytes = memoryStream.ToArray();
        }

        await rule.ImportExcelAsync(_context, bytes);
        return Ok(_context.Data.ToListAsync());
    }

    [HttpPost("import-file-data-csv")]
    public async Task<IActionResult> GetFileBytes(string file)
    {
        var rule = new FileDataImportRule();
        await rule.ImportCsvAsync(_context, file);
        return Ok(_context.Data.ToListAsync());
    }
}