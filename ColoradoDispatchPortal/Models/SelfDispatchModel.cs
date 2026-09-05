using System.ComponentModel.DataAnnotations;

namespace ColoradoDispatchPortal.Models;

public class SelfDispatchModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Reference Number")]
    public string ReferenceNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    public string? CancellationType { get; set; }
    public DateTime? ReceivedDateTime { get; set; }
    public DateTime? DispatchDateTime { get; set; }
    public DateTime? ClearedFromOnSceneDateTime { get; set; }
    public string? RespondingClinicianTeam { get; set; }
    public string? Disposition { get; set; }
    public string? ResponseLocation { get; set; }

    public bool BHAppointmentOffered { get; set; }
    public string? WhyNotBHAppointment { get; set; }
    public bool OfferedWithin7Days { get; set; }
    public bool AppointmentScheduled { get; set; }
    public bool ScheduledWithin7Days { get; set; }
    public string? WhyNotScheduled { get; set; }
    public bool FollowUpOffered { get; set; }
    public bool FiveDayFollowUpOffered { get; set; }
    public bool IsComplete { get; set; }

    [Range(1, int.MaxValue)]
    public int ProviderId { get; set; }
    public string? ProviderName { get; set; }

    public List<FollowUpModel> FollowUps { get; set; } = new();
    public DateTime? FiveDayFollowUpDateTime { get; set; }
    public string? FiveDayFollowUpOutcome { get; set; }
}
