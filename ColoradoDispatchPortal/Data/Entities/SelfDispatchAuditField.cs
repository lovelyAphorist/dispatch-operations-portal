namespace ColoradoDispatchPortal.Data.Entities;

public class SelfDispatchAuditField
{
    public int Id { get; set; }
    public int AuditHistoryId { get; set; }
    public SelfDispatchAuditHistory AuditHistory { get; set; } = null!;

    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
