using ProfisysTask.DTOs;
using ProfisysTask.Models;
using ProfisysTask.Services.Csv;

namespace ProfisysTask.Mapping;

public static class DocumentMappingExtensions
{
    public static DocumentListItemDto ToListItemDto(this Document d) => new()
    {
        Id = d.Id,
        Type = d.Type,
        Date = d.Date,
        FirstName = d.FirstName,
        LastName = d.LastName,
        City = d.City,
    };

    public static DocumentDetailDto ToDetailDto(this Document d) => new()
    {
        Id = d.Id,
        Type = d.Type,
        Date = d.Date,
        FirstName = d.FirstName,
        LastName = d.LastName,
        City = d.City,
        Items = d.Items
            .OrderBy(i => i.Ordinal)
            .Select(i => i.ToDto())
            .ToList(),
    };

    public static DocumentItemDto ToDto(this DocumentItem i) => new()
    {
        Id = i.Id,
        Ordinal = i.Ordinal,
        Product = i.Product,
        Quantity = i.Quantity,
        Price = i.Price,
        TaxRate = i.TaxRate,
    };

    public static Document ToEntity(this DocumentCsvRow row) => new()
    {
        Id = row.Id,
        Type = (row.Type ?? string.Empty).Trim(),
        Date = row.Date,
        FirstName = (row.FirstName ?? string.Empty).Trim(),
        LastName = (row.LastName ?? string.Empty).Trim(),
        City = (row.City ?? string.Empty).Trim(),
    };

    public static DocumentItem ToEntity(this DocumentItemCsvRow row) => new()
    {
        DocumentId = row.DocumentId,
        Ordinal = row.Ordinal,
        Product = (row.Product ?? string.Empty).Trim(),
        Quantity = row.Quantity,
        Price = row.Price,
        TaxRate = row.TaxRate,
    };
}
