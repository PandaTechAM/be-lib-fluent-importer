using FluentImporter.Demo.ImportModels;
using FluentImporter.Demo.Models;
using FluentImporter.Metadata;
using FluentImporter.Templates;
using FluentMinimalApiMapper;

namespace FluentImporter.Demo.Endpoints;

/// <summary>
///     Download the generated template, inspect the metadata it is generated from, and upload a filled copy back.
/// </summary>
public class EstateTemplateEndpoint : IEndpoint
{
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly Dictionary<string, string> ArmenianEnumLabels = new()
    {
        ["Apartment"] = "Բնակարան",
        ["CommercialArea"] = "Կոմերցիոն տարածք",
        ["ParkingArea"] = "Կայանման տարածք",
        ["Basement"] = "Նկուղ",
        ["House"] = "Տուն",
        ["Other"] = "Այլ"
    };

    private static readonly Dictionary<string, string> ArmenianColumnDescriptions = new()
    {
        ["Building Id"] = "Շենքի ID",
        ["Type"] = "Գույքի տեսակը",
        ["Address Number"] = "Բնակարանի համարը",
        ["Floor"] = "Հարկը",
        ["Sqm"] = "Մակերեսը (ք.մ.)",
        ["Commissioning Date"] = "Շահագործման ամսաթիվը",
        ["Note"] = "Նշում"
    };

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/estates/template", (string? lang) =>
        {
            var bytes = new EstateImportRule().BuildTemplate(options: BuildOptions(lang));
            return Results.File(bytes, Xlsx, "estates-import-template.xlsx");
        });

        app.MapGet("/estates/columns", () => new EstateImportRule()
            .Columns
            .Select(c => new
            {
                c.ColumnName,
                c.PropertyName,
                Type = c.PropertyType.Name,
                c.IsRequired,
                c.IsNullable,
                c.RegexPattern,
                c.HasCustomConverter,
                c.Description,
                c.ExpectedFormat,
                c.Example,
                EnumSource = c.EnumSource?.Name,
                ValueSource = c.ValueSource.ToString()
            }));

        app.MapPost("/estates/import", async (IFormFile file, CancellationToken ct) =>
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, ct);
            stream.Position = 0;

            var result = new EstateImportRule().TryReadXlsx(stream, new ImportReadOptions { SheetName = null });

            return result.HasErrors
                ? Results.BadRequest(new { result.Errors, Parsed = result.Records.Count })
                : Results.Ok(result.Records);
        })
        .DisableAntiforgery();
    }

    private static ImportTemplateOptions BuildOptions(string? lang)
    {
        if (!string.Equals(lang, "hy", StringComparison.OrdinalIgnoreCase))
        {
            return new ImportTemplateOptions();
        }

        return new ImportTemplateOptions
        {
            DataSheetName = "Գույքեր",
            LegendSheetName = "Բացատրություն",
            EnumLabelResolver = (_, member, _) => ArmenianEnumLabels.GetValueOrDefault(member),
            ColumnDescriptionResolver = column => ArmenianColumnDescriptions.GetValueOrDefault(column.ColumnName),
            Texts = new ImportTemplateTexts
            {
                LegendTitle = "Բացատրություն",
                ColumnsTitle = "Սյունակներ",
                AllowedValuesTitle = "Թույլատրելի արժեքներ",
                ColumnHeader = "Սյունակ",
                RequiredHeader = "Պարտադիր",
                TypeHeader = "Տեսակ",
                FormatHeader = "Ձևաչափ",
                DescriptionHeader = "Նկարագիր",
                ValueHeader = "Արժեք",
                LabelHeader = "Նկարագիր",
                RequiredYes = "Այո",
                RequiredNo = "Ոչ",
                RequiredNote = "Պարտադիր",
                ListType = "Ընտրել ցանկից",
                WholeNumberType = "Ամբողջ թիվ",
                DecimalNumberType = "Տասնորդական թիվ",
                TextType = "Տեքստ",
                DateType = "Ամսաթիվ",
                BooleanType = "Այո / Ոչ",
                InvalidValueTitle = "Սխալ արժեք",
                InvalidValueMessage = "Ընտրեք արժեք ցանկից։ Տես «Բացատրություն» էջը։"
            }
        };
    }
}
