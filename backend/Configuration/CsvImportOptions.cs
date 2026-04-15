namespace ProfisysTask.Configuration;

public class CsvImportOptions
{
    public const string SectionName = "CsvImport";

    public string Delimiter { get; set; } = ";";
    public int BatchSize { get; set; } = 500;
    public string Culture { get; set; } = "pl-PL";
    public long MaxUploadBytes { get; set; } = 100 * 1024 * 1024;
}
