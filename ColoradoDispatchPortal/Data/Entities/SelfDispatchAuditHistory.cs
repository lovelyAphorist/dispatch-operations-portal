namespace ColoradoDispatchPortal.Data.Entities;

public class SelfDispatchAuditHistory
{
    public int Id { get; set; }
    public int DispatchId { get; set; }
    public SelfDispatch Dispatch { get; set; } = null!;

    public DateTime ChangedDate { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;

    public ICollection<SelfDispatchAuditField> Fields { get; set; } = new List<SelfDispatchAuditField>();
}
