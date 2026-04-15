namespace ProfisysTask.Models;

public class DocumentItem
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int Ordinal { get; set; }
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public int TaxRate { get; set; }
    public Document Document { get; set; } = null!;
}
