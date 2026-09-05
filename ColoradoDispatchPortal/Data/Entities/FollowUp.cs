namespace ColoradoDispatchPortal.Data.Entities;

public class FollowUp
{
    public int Id { get; set; }
    public int DispatchId { get; set; }
    public SelfDispatch Dispatch { get; set; } = null!;

    public DateTime? DateTime { get; set; }
    public string? Outcome { get; set; }
    public string? Who { get; set; }
}
