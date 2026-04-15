namespace ProfisysTask.DTOs;

public class DocumentDetailDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public List<DocumentItemDto> Items { get; set; } = new();
}
