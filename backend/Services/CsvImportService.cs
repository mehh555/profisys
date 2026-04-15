using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProfisysTask.Configuration;
using ProfisysTask.Data;
using ProfisysTask.DTOs;
using ProfisysTask.Exceptions;
using ProfisysTask.Mapping;
using ProfisysTask.Models;
using ProfisysTask.Services.Csv;

namespace ProfisysTask.Services;

public class CsvImportService : ICsvImportService
{
    private static readonly string[] RequiredDocumentHeaders = ["Id", "Type", "Date", "FirstName", "LastName", "City"];
    private static readonly string[] RequiredItemHeaders = ["DocumentId", "Ordinal", "Product", "Quantity", "Price", "TaxRate"];

    private readonly AppDbContext _context;
    private readonly ILogger<CsvImportService> _logger;
    private readonly CsvImportOptions _options;

    public CsvImportService(AppDbContext context, ILogger<CsvImportService> logger, IOptions<CsvImportOptions> options)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<ImportResultDto> ImportAsync(Stream documentsStream, Stream documentItemsStream, CancellationToken cancellationToken = default)
    {
        var culture = CultureInfo.GetCultureInfo(_options.Culture);
        var csvConfig = new CsvConfiguration(culture)
        {
            Delimiter = _options.Delimiter,
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => args.Header.Trim(),
        };

        var result = new ImportResultDto();
        var importedDocIds = new HashSet<int>();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await ImportDocumentsAsync(documentsStream, csvConfig, importedDocIds, result, cancellationToken);
            await DeleteItemsForDocumentsAsync(importedDocIds, cancellationToken);
            await ImportItemsAsync(documentItemsStream, csvConfig, importedDocIds, result, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Import was cancelled by the client");
            throw;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        _logger.LogInformation("Import completed: {Imported} imported, {Updated} updated, {Skipped} orphaned items skipped",
            result.Imported, result.Updated, result.SkippedItems);

        return result;
    }

    private async Task ImportDocumentsAsync(
        Stream stream,
        CsvConfiguration config,
        HashSet<int> importedDocIds,
        ImportResultDto result,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();
        ValidateHeaders(csv.HeaderRecord, RequiredDocumentHeaders, "Documents");

        var errors = new List<string>();
        var batch = new List<DocumentCsvRow>(_options.BatchSize);

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                batch.Add(csv.GetRecord<DocumentCsvRow>());
            }
            catch (Exception ex)
            {
                errors.Add($"Documents row {csv.Context.Parser!.Row}: {ex.Message}");
                continue;
            }

            if (batch.Count >= _options.BatchSize)
            {
                await FlushDocumentsBatchAsync(batch, importedDocIds, result, cancellationToken);
                batch.Clear();
            }
        }

        if (errors.Count > 0)
            throw new CsvImportException(errors);

        if (batch.Count > 0)
            await FlushDocumentsBatchAsync(batch, importedDocIds, result, cancellationToken);

        if (importedDocIds.Count == 0)
            _logger.LogWarning("Documents CSV file contained no data rows");
    }

    private async Task FlushDocumentsBatchAsync(
        IReadOnlyList<DocumentCsvRow> batch,
        HashSet<int> importedDocIds,
        ImportResultDto result,
        CancellationToken cancellationToken)
    {
        var duplicates = batch
            .GroupBy(r => r.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .Concat(batch.Where(r => importedDocIds.Contains(r.Id)).Select(r => r.Id))
            .Distinct()
            .ToList();

        if (duplicates.Count > 0)
            throw new CsvImportException(
                duplicates.Select(id => $"Documents: duplicate Id {id} in CSV.").ToList());

        var ids = batch.Select(r => r.Id).ToList();
        var existing = await _context.Documents
            .Where(d => ids.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, cancellationToken);

        foreach (var row in batch)
        {
            if (existing.TryGetValue(row.Id, out var tracked))
            {
                tracked.Type = row.Type.Trim();
                tracked.Date = row.Date;
                tracked.FirstName = row.FirstName.Trim();
                tracked.LastName = row.LastName.Trim();
                tracked.City = row.City.Trim();
                result.Updated++;
            }
            else
            {
                await _context.Documents.AddAsync(row.ToEntity(), cancellationToken);
                result.Imported++;
            }

            importedDocIds.Add(row.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteItemsForDocumentsAsync(IReadOnlyCollection<int> documentIds, CancellationToken cancellationToken)
    {
        if (documentIds.Count == 0) return;

        await _context.DocumentItems
            .Where(i => documentIds.Contains(i.DocumentId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task ImportItemsAsync(
        Stream stream,
        CsvConfiguration config,
        HashSet<int> importedDocIds,
        ImportResultDto result,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();
        ValidateHeaders(csv.HeaderRecord, RequiredItemHeaders, "DocumentItems");

        var errors = new List<string>();
        var batch = new List<DocumentItem>(_options.BatchSize);

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            DocumentItemCsvRow row;
            try
            {
                row = csv.GetRecord<DocumentItemCsvRow>();
            }
            catch (Exception ex)
            {
                errors.Add($"DocumentItems row {csv.Context.Parser!.Row}: {ex.Message}");
                continue;
            }

            if (!importedDocIds.Contains(row.DocumentId))
            {
                result.SkippedItems++;
                continue;
            }

            batch.Add(row.ToEntity());

            if (batch.Count >= _options.BatchSize)
            {
                await _context.DocumentItems.AddRangeAsync(batch, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                batch.Clear();
            }
        }

        if (errors.Count > 0)
            throw new CsvImportException(errors);

        if (batch.Count > 0)
        {
            await _context.DocumentItems.AddRangeAsync(batch, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (result.SkippedItems > 0)
            _logger.LogWarning("Skipped {Count} DocumentItems row(s) referencing non-existent document IDs", result.SkippedItems);
    }

    private static void ValidateHeaders(string[]? actual, string[] required, string fileName)
    {
        if (actual is null || actual.Length == 0)
            throw new CsvImportException([$"{fileName}: file is empty or has no header row."]);

        var missing = required.Where(h => !actual.Contains(h, StringComparer.OrdinalIgnoreCase)).ToList();
        if (missing.Count > 0)
            throw new CsvImportException([$"{fileName}: missing required column(s): {string.Join(", ", missing)}"]);
    }
}
