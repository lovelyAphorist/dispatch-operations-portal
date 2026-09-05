namespace ColoradoDispatchPortal.Data.Entities;

public class SelfDispatch
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? CancellationType { get; set; }
    public DateTime? ReceivedDateTime { get; set; }
    public DateTime? DispatchDateTime { get; set; }
    public DateTime? ClearedFromOnSceneDateTime { get; set; }
    public string? RespondingClinicianTeam { get; set; }
    public string? Disposition { get; set; }
    public string? ResponseLocation { get; set; }

    public bool? BHAppointmentOffered { get; set; }
    public string? WhyNotBHAppointment { get; set; }
    public bool? OfferedWithin7Days { get; set; }
    public bool? AppointmentScheduled { get; set; }
    public bool? ScheduledWithin7Days { get; set; }
    public string? WhyNotScheduled { get; set; }
    public bool? FollowUpOffered { get; set; }
    public bool? FiveDayFollowUpOffered { get; set; }
    public bool IsComplete { get; set; }

    public int ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public SelfDispatchFiveDayFollowUp? FiveDayFollowUp { get; set; }
    public ICollection<SelfDispatchAuditHistory> AuditHistories { get; set; } = new List<SelfDispatchAuditHistory>();
}
