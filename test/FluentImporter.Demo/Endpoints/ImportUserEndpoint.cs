using FluentImporter.Demo.Excel;
using FluentMinimalApiMapper;

namespace FluentImporter.Demo.Endpoints;

public class ImportUserEndpoint : IEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/user/xlsx", ExcelSupport.ReadExcelFile);
    }
}