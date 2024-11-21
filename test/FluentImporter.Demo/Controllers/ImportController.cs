﻿using FluentImporter.Demo.Context;
using FluentImporter.Demo.ImportModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace FluentImporter.Demo.Controllers;

[ApiController]
[Route("api/v1")]
public class ImportController : ControllerBase
{
    [HttpPost("import-file-data-dictionary")]
    public async Task<IActionResult> ImportFileData()
    {
        var data = new List<Dictionary<string, string>>
        {
            new()
            {
                { "Id", "1" },
                { "Nullable Id", "1" },
                { "Sqm", "1.25" },
                { "Name", null },
                { "Date", null },
                { "Description", "Description1" },
                { "Comment", "Comment1" },
                { "CreatedAt", "2021-01-01" },
                { "CreatedBy", "System" }
            },
            new()
            {
                { "Id", "3" },
                { "Nullable Id", "2" },
                { "Sqm", null },
                { "Name", "Name1" },
                { "Date", "2025-01-01" },
                { "Description", "Description1" },
                { "Comment", "Comment1" },
                { "CreatedAt", "2021-01-01" },
                { "CreatedBy", "System" }
            },
            new()
            {
                { "Id", "d4" },
                { "Nullable Id", null },
                { "Sqm", "999.18" },
                { "Name", "Name1" },
                { "Date", null },
                { "Description", "Description1" },
                { "Comment", "aaa" },
                { "CreatedAt", "2021-01-01" },
                { "CreatedBy", "System" }
            }
        };

        var rule = new FileDataImportRule();

        var records = rule.GetRecords(data);

        return Ok(records);
    }

    [HttpPost("import-file-data-xlsx")]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        var rule = new FileDataImportRule2();

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        var records = rule.ReadXlsx(memoryStream).ToList();

        return Ok(records);
    }

    [HttpPost("import-file-data-xlsx-path")]
    public async Task<IActionResult> ImportExcel(string file)
    {
        var rule = new FileDataImportRule2();

        var records = rule.ReadXlsx(file).ToList();

        return Ok(records);
    }

    [HttpPost("import-file-data-csv-path")]
    public async Task<IActionResult> ImportCsv(string file)
    {
        var rule = new FileDataImportRule();

        var records = rule.ReadCsv(file);

        return Ok(records);
    }

    [HttpPost("import-file-data-csv")]
    public async Task<IActionResult> ImportCsv(IFormFile file)
    {
        var rule = new FileDataImportRule2();

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        
        var records = rule.ReadCsv(memoryStream);

        return Ok(records);
    }
}