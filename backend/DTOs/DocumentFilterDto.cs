using System.ComponentModel.DataAnnotations;

namespace ProfisysTask.DTOs;

public class DocumentFilterDto
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}
