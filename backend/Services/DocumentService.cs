using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProfisysTask.Configuration;
using ProfisysTask.Data;
using ProfisysTask.DTOs;
using ProfisysTask.Mapping;
using ProfisysTask.Models;

namespace ProfisysTask.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _context;
    private readonly CsvImportOptions _options;

    public DocumentService(AppDbContext context, IOptions<CsvImportOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<(List<DocumentListItemDto> Items, int TotalCount)> GetDocumentsAsync(
        DocumentFilterDto filter,
        bool applyPaging = true,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(_context.Documents.AsNoTracking(), filter);

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, filter);

        if (applyPaging)
        {
            query = query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize);
        }

        var documents = await query.ToListAsync(cancellationToken);
        return (documents.Select(d => d.ToListItemDto()).ToList(), totalCount);
    }

    public async Task<DocumentDetailDto?> GetDocumentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return document?.ToDetailDto();
    }

    public async Task<Stream> ExportAsCsvAsync(DocumentFilterDto filter, CancellationToken cancellationToken = default)
    {
        var (items, _) = await GetDocumentsAsync(filter, applyPaging: false, cancellationToken);

        var culture = CultureInfo.GetCultureInfo(_options.Culture);
        var config = new CsvConfiguration(culture)
        {
            Delimiter = _options.Delimiter,
            HasHeaderRecord = true,
        };

        var stream = new MemoryStream();
        await using (var writer = new StreamWriter(stream, leaveOpen: true))
        await using (var csv = new CsvWriter(writer, config))
        {
            csv.WriteHeader<DocumentListItemDto>();
            await csv.NextRecordAsync();

            foreach (var doc in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                csv.WriteRecord(doc);
                await csv.NextRecordAsync();
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static IQueryable<Document> ApplyFilters(IQueryable<Document> query, DocumentFilterDto filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search}%";
            query = query.Where(d =>
                EF.Functions.Like(d.FirstName, pattern) ||
                EF.Functions.Like(d.LastName, pattern) ||
                EF.Functions.Like(d.City, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filter.Type))
            query = query.Where(d => d.Type == filter.Type);

        if (filter.DateFrom.HasValue)
            query = query.Where(d => d.Date >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(d => d.Date <= filter.DateTo.Value);

        return query;
    }

    private static IQueryable<Document> ApplySorting(IQueryable<Document> query, DocumentFilterDto filter)
    {
        var descending = string.Equals(filter.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return (filter.SortBy?.ToLowerInvariant()) switch
        {
            "type" => descending ? query.OrderByDescending(d => d.Type) : query.OrderBy(d => d.Type),
            "date" => descending ? query.OrderByDescending(d => d.Date) : query.OrderBy(d => d.Date),
            "firstname" => descending ? query.OrderByDescending(d => d.FirstName) : query.OrderBy(d => d.FirstName),
            "lastname" => descending ? query.OrderByDescending(d => d.LastName) : query.OrderBy(d => d.LastName),
            "city" => descending ? query.OrderByDescending(d => d.City) : query.OrderBy(d => d.City),
            _ => descending ? query.OrderByDescending(d => d.Id) : query.OrderBy(d => d.Id),
        };
    }
}
