namespace ColoradoDispatchPortal.Models;

public class DashboardViewModel
{
    public int SelectedDemoUserId { get; set; }
    public string SelectedDemoUserName { get; set; } = string.Empty;
    public string SelectedRole { get; set; } = string.Empty;
    public string AccessSummary { get; set; } = string.Empty;
    public IReadOnlyList<DemoUserOption> DemoUsers { get; set; } = Array.Empty<DemoUserOption>();
}

public class DemoUserOption
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
