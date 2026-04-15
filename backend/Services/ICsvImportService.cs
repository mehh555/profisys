using ProfisysTask.DTOs;

namespace ProfisysTask.Services;

public interface ICsvImportService
{
    Task<ImportResultDto> ImportAsync(Stream documentsStream, Stream documentItemsStream, CancellationToken cancellationToken = default);
}
