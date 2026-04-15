using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProfisysTask.Configuration;
using ProfisysTask.DTOs;
using ProfisysTask.Exceptions;
using ProfisysTask.Services;

namespace ProfisysTask.Controllers;

[ApiController]
[Route("api")]
public class DocumentsController : ControllerBase
{
    private static readonly string[] AllowedCsvMimeTypes =
    [
        "text/csv",
        "application/csv",
        "application/vnd.ms-excel",
        "text/plain",
        "application/octet-stream",
    ];

    private readonly IDocumentService _documentService;
    private readonly ICsvImportService _csvImportService;
    private readonly CsvImportOptions _options;

    public DocumentsController(
        IDocumentService documentService,
        ICsvImportService csvImportService,
        IOptions<CsvImportOptions> options)
    {
        _documentService = documentService;
        _csvImportService = csvImportService;
        _options = options.Value;
    }

    [HttpPost("import")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
    public async Task<IActionResult> Import(
        [FromForm(Name = "documents")] IFormFile? documentsFile,
        [FromForm(Name = "documentItems")] IFormFile? documentItemsFile,
        CancellationToken cancellationToken)
    {
        if (documentsFile is null || documentItemsFile is null)
            throw new BadRequestException("Both 'documents' and 'documentItems' files are required.");

        EnsureValidCsv(documentsFile, nameof(documentsFile));
        EnsureValidCsv(documentItemsFile, nameof(documentItemsFile));

        await using var documentsStream = documentsFile.OpenReadStream();
        await using var itemsStream = documentItemsFile.OpenReadStream();

        var result = await _csvImportService.ImportAsync(documentsStream, itemsStream, cancellationToken);
        return Ok(result);
    }

    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments([FromQuery] DocumentFilterDto filter, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _documentService.GetDocumentsAsync(filter, applyPaging: true, cancellationToken);

        return Ok(new
        {
            items,
            totalCount,
            page = filter.Page,
            pageSize = filter.PageSize
        });
    }

    [HttpGet("documents/{id:int}")]
    public async Task<IActionResult> GetDocument(int id, CancellationToken cancellationToken)
    {
        var document = await _documentService.GetDocumentByIdAsync(id, cancellationToken);

        if (document is null)
            return NotFound();

        return Ok(document);
    }

    [HttpGet("documents/export")]
    public async Task<IActionResult> ExportDocuments([FromQuery] DocumentFilterDto filter, CancellationToken cancellationToken)
    {
        var stream = await _documentService.ExportAsCsvAsync(filter, cancellationToken);
        return File(stream, "text/csv", "documents_export.csv");
    }

    private void EnsureValidCsv(IFormFile file, string fieldName)
    {
        if (file.Length == 0)
            throw new BadRequestException($"'{fieldName}' is empty.");

        if (file.Length > _options.MaxUploadBytes)
            throw new BadRequestException($"'{fieldName}' exceeds the maximum allowed size of {_options.MaxUploadBytes} bytes.");

        var hasCsvExtension = Path.GetExtension(file.FileName)
            .Equals(".csv", StringComparison.OrdinalIgnoreCase);

        var hasAllowedMime = !string.IsNullOrWhiteSpace(file.ContentType)
            && AllowedCsvMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase);

        if (!hasCsvExtension || !hasAllowedMime)
            throw new BadRequestException($"'{fieldName}' must be a CSV file (.csv, text/csv).");
    }
}
