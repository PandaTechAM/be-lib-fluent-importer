using System.Globalization;
using FluentImporter.Demo.Models;

namespace FluentImporter.Demo.ImportModels;

/// <summary>
///     Mirrors a real-world rule: an <c>int</c> column carrying an enum, a converter with a declared textual format,
///     and a required/optional mix. Drives <c>/estates/template</c>.
/// </summary>
public class EstateImportRule : ImportRule<EstateImportModel>
{
    public EstateImportRule()
    {
        RuleFor(x => x.BuildingId)
            .ReadFromColumn("Building Id")
            .NotEmpty()
            .Describe("Id of the building this estate belongs to")
            .WithExample(1);

        RuleFor(x => x.Type)
            .ReadFromColumn("Type")
            .NotEmpty()
            .WithEnumSource<EstateType>()
            .Describe("Estate type. Pick from the dropdown.")
            .WithExample(0);

        RuleFor(x => x.AddressNumber)
            .ReadFromColumn("Address Number")
            .NotEmpty()
            .Describe("Apartment or unit number")
            .WithExample("12a");

        RuleFor(x => x.Floor)
            .ReadFromColumn("Floor")
            .Describe("Floor number")
            .WithExample(3);

        RuleFor(x => x.Sqm)
            .ReadFromColumn("Sqm")
            .NotEmpty()
            .Convert(x => decimal.Parse(x, CultureInfo.InvariantCulture))
            .Describe("Area in square metres")
            .WithExample(54.5m);

        RuleFor(x => x.CommissioningDate)
            .ReadFromColumn("Commissioning Date")
            .Convert(x => DateTime.ParseExact(x, "dd/MM/yyyy", CultureInfo.InvariantCulture))
            .WithExpectedFormat("dd/MM/yyyy")
            .Describe("Date the building was commissioned")
            .WithExample(new DateTime(2019, 4, 23));

        RuleFor(x => x.Note)
            .ReadFromColumn("Note")
            .Describe("Free-text note");

        RuleFor(x => x.ImportedAt)
            .WriteValue(DateTime.UtcNow);
    }
}
