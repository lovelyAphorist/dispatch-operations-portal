namespace ColoradoDispatchPortal.Data.Entities;

public class Provider
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<SelfDispatch> SelfDispatches { get; set; } = new List<SelfDispatch>();
    public ICollection<UserProvider> UserProviders { get; set; } = new List<UserProvider>();
}
