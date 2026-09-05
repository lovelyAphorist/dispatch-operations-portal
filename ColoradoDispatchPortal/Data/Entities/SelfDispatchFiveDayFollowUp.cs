namespace ColoradoDispatchPortal.Data.Entities;

public class SelfDispatchFiveDayFollowUp
{
    public int Id { get; set; }
    public int DispatchId { get; set; }
    public SelfDispatch Dispatch { get; set; } = null!;

    public DateTime? DateTime { get; set; }
    public string? Outcome { get; set; }
}
