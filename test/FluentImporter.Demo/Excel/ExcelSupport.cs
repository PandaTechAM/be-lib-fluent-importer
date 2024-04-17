using System.Text.Json;
using ClosedXML.Excel;
using FluentImporter.Demo.Models;

namespace FluentImporter.Demo.Excel;

public static class ExcelSupport
{
    public static string ReadExcelFile()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(baseDir, "Files", "users.xlsx");
        // Load the Excel workbook
        using var workbook = new XLWorkbook(filePath);

        // Get the first worksheet
        var worksheet = workbook.Worksheet(1);

        var users = new List<User>();

        // Assuming the first row contains headers and data starts from the second row
        bool firstRow = true;
        foreach (var row in worksheet.Rows())
        {
            if (firstRow)
            {
                firstRow = false;
                continue;
            }

            var user = new User
            {
                Id = long.Parse(row.Cell("A").Value.ToString() ?? "0"),
                Name = row.Cell("B").Value.ToString() ?? string.Empty,
                CreatedAt = DateTime.Parse(row.Cell("C").Value.ToString() ?? DateTime.MinValue.ToString())
            };

            users.Add(user);
        }

        // Convert the list of users to JSON
        var options = new JsonSerializerOptions { WriteIndented = true }; // To make the JSON output more readable
        string json = JsonSerializer.Serialize(users, options);

        // Write to console
        return json;
    }
    
}