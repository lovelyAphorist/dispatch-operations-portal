namespace ColoradoDispatchPortal.Models;

public class FilterModel
{
    public string Field { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Value { get; set; }
}
