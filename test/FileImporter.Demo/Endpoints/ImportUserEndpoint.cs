using Carter;
using FileImporter.Demo.Excel;

namespace FileImporter.Demo.Endpoints;

public class ImportUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/user/xlsx", ExcelSupport.ReadExcelFile);
    }
}