using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PandaTech.ServiceResponse;

namespace Pandatech.TestApi.Controllers;

[ApiController]
[Route("api/v1")]
public class ImportController : ControllerBase
{
    MyContext _context;

    public ImportController(IExceptionHandler exceptionHandler)
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
        var data = new List<Dictionary<string, string>>()
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

        await rule.ImportFromExcelAsync(_context, bytes);
        return Ok(_context.Data.ToListAsync());
    }

    [HttpPost("import-file-data-csv")]
    public async Task<IActionResult> GetFileBytes(string file)
    {
        var rule = new FileDataImportRule();
        await rule.ImportFromCsvAsync(_context, file);
        return Ok(_context.Data.ToListAsync());
    }
}