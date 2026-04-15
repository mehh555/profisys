namespace ProfisysTask.Exceptions;

public class CsvImportException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public CsvImportException(IReadOnlyList<string> errors)
        : base($"CSV import failed with {errors.Count} error(s).")
    {
        Errors = errors;
    }
}
