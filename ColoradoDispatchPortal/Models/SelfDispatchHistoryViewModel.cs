namespace ColoradoDispatchPortal.Models;

public class SelfDispatchHistoryViewModel
{
    public DateTime ChangedDate { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
