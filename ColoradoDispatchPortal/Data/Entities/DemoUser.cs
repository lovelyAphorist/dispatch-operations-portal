namespace ColoradoDispatchPortal.Data.Entities;

public class DemoUser
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public ICollection<UserProvider> UserProviders { get; set; } = new List<UserProvider>();
}
