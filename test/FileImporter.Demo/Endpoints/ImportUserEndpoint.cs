using FileImporter.Demo.Excel;
using FluentMinimalApiMapper;

namespace FileImporter.Demo.Endpoints;

public class ImportUserEndpoint : IEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/user/xlsx", ExcelSupport.ReadExcelFile);
    }
}