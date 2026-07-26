namespace FluentImporter.Demo.Models;

public enum EstateType
{
    Apartment = 0,
    CommercialArea = 1,
    ParkingArea = 2,
    Basement = 3,
    House = 4,
    Other = 5
}

public class EstateImportModel
{
    public long BuildingId { get; set; }
    public int Type { get; set; }
    public string AddressNumber { get; set; } = null!;
    public long? Floor { get; set; }
    public decimal Sqm { get; set; }
    public DateTime? CommissioningDate { get; set; }
    public string? Note { get; set; }
    public DateTime ImportedAt { get; set; }
}
