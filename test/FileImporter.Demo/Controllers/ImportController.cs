using FileImporter.Demo.Context;
using FileImporter.Demo.ImportModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FileImporter.Demo.Controllers;

[ApiController]
[Route("api/v1")]
public class ImportController(MyDbContext context) : ControllerBase
{
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
                { "Comment", "Comment1" },
                { "CreatedAt", "2021-01-01" },
                { "CreatedBy", "System" }
            },
            new()
            {
                { "Id", "3" },
                { "Name", "Name1" },
                { "Description", "Description1" },
                { "Comment", "Comment1" },
                { "CreatedAt", "2021-01-01" },
                { "CreatedBy", "System" }
            },
            new()
            {
                { "Id", "d4" },
                { "Name", "Name1" },
                { "Description", "Description1" },
                { "Comment", "Comment1" },
                { "CreatedAt", "2021-01-01" },
                { "CreatedBy", "System" }
            }
        };

        var rule = new FileDataImportRule();
        await rule.ImportAsync(context, data);
        return Ok(context.FileData.ToListAsync());
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

        await rule.ImportExcelAsync(context, bytes);
        return Ok(context.FileData.ToListAsync());
    }

    [HttpPost("import-file-data-csv")]
    public async Task<IActionResult> GetFileBytes(string file)
    {
        var rule = new FileDataImportRule();
        await rule.ImportCsvAsync(context, file);
        return Ok(context.FileData.ToListAsync());
    }
}