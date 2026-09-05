namespace ColoradoDispatchPortal.Data.Entities;

public class UserProvider
{
    public int DemoUserId { get; set; }
    public DemoUser DemoUser { get; set; } = null!;

    public int ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;
}
