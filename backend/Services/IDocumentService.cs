using ProfisysTask.DTOs;

namespace ProfisysTask.Services;

public interface IDocumentService
{
    Task<(List<DocumentListItemDto> Items, int TotalCount)> GetDocumentsAsync(
        DocumentFilterDto filter,
        bool applyPaging = true,
        CancellationToken cancellationToken = default);

    Task<DocumentDetailDto?> GetDocumentByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Stream> ExportAsCsvAsync(DocumentFilterDto filter, CancellationToken cancellationToken = default);
}
