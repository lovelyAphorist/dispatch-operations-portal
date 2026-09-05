using AutoMapper;
using ColoradoDispatchPortal.Data.Entities;
using ColoradoDispatchPortal.Models;

namespace ColoradoDispatchPortal.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<FollowUp, FollowUpModel>().ReverseMap();

        CreateMap<SelfDispatch, SelfDispatchModel>()
            .ForMember(d => d.ProviderName, o => o.MapFrom(s => s.Provider.Name))
            .ForMember(d => d.BHAppointmentOffered, o => o.MapFrom(s => s.BHAppointmentOffered ?? false))
            .ForMember(d => d.OfferedWithin7Days, o => o.MapFrom(s => s.OfferedWithin7Days ?? false))
            .ForMember(d => d.AppointmentScheduled, o => o.MapFrom(s => s.AppointmentScheduled ?? false))
            .ForMember(d => d.ScheduledWithin7Days, o => o.MapFrom(s => s.ScheduledWithin7Days ?? false))
            .ForMember(d => d.FollowUpOffered, o => o.MapFrom(s => s.FollowUpOffered ?? false))
            .ForMember(d => d.FiveDayFollowUpOffered, o => o.MapFrom(s => s.FiveDayFollowUpOffered ?? false))
            .ForMember(d => d.FiveDayFollowUpDateTime, o => o.MapFrom(s => s.FiveDayFollowUp == null ? null : s.FiveDayFollowUp.DateTime))
            .ForMember(d => d.FiveDayFollowUpOutcome, o => o.MapFrom(s => s.FiveDayFollowUp == null ? null : s.FiveDayFollowUp.Outcome));

        CreateMap<SelfDispatchModel, SelfDispatch>()
            .ForMember(d => d.Provider, o => o.Ignore())
            .ForMember(d => d.FollowUps, o => o.Ignore())
            .ForMember(d => d.FiveDayFollowUp, o => o.Ignore())
            .ForMember(d => d.AuditHistories, o => o.Ignore());
    }
}
