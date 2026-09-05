using ColoradoDispatchPortal.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ColoradoDispatchPortal.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(DispatchPortalContext db)
    {
        if (await db.SelfDispatches.AnyAsync())
        {
            return;
        }

        var providers = new[]
        {
            new Provider { Id = 1, Name = "Demo Provider North" },
            new Provider { Id = 2, Name = "Demo Provider Central" },
            new Provider { Id = 3, Name = "Demo Provider West" }
        };

        var users = new[]
        {
            new DemoUser { Id = 1, DisplayName = "Avery Agency Admin", Role = "AGENCYADMIN" },
            new DemoUser { Id = 2, DisplayName = "Bailey Agency Staff", Role = "AGENCYSTAFF" },
            new DemoUser { Id = 3, DisplayName = "Casey Auditor", Role = "AUDITOR" },
            new DemoUser { Id = 4, DisplayName = "Drew Internal Admin", Role = "INTERNALADMIN" },
            new DemoUser { Id = 5, DisplayName = "Ellis Dispatch", Role = "INTERNALDISPATCH" }
        };

        db.Providers.AddRange(providers);
        db.DemoUsers.AddRange(users);
        db.UserProviders.AddRange(
            new UserProvider { DemoUserId = 1, ProviderId = 1 },
            new UserProvider { DemoUserId = 1, ProviderId = 2 },
            new UserProvider { DemoUserId = 2, ProviderId = 1 },
            new UserProvider { DemoUserId = 3, ProviderId = 2 });

        var firstNames = new[] { "Jordan", "Taylor", "Morgan", "Riley", "Cameron", "Quinn", "Parker", "Skyler" };
        var lastNames = new[] { "Adams", "Brooks", "Carter", "Diaz", "Evans", "Foster", "Gray", "Hayes" };
        var dispositions = new[] { "Community Support", "Stabilized On Scene", "Transported", "Referred", "Follow-Up Planned" };
        var locations = new[] { "Residence", "Community Site", "School", "Clinic", "Public Location" };
        var teams = new[] { "North Team A", "Central Team B", "West Team C" };

        var now = DateTime.UtcNow;
        for (var i = 1; i <= 36; i++)
        {
            var providerId = ((i - 1) % 3) + 1;
            var received = now.AddDays(-(i * 2)).AddHours(-(i % 10));
            var isComplete = i % 4 != 0;

            var dispatch = new SelfDispatch
            {
                ReferenceNumber = $"CO-DEMO-{i:0000}",
                FirstName = firstNames[i % firstNames.Length],
                LastName = lastNames[(i * 3) % lastNames.Length],
                CancellationType = i % 7 == 0 ? "Client declined" : null,
                ReceivedDateTime = received,
                DispatchDateTime = received.AddMinutes(15 + (i % 20)),
                ClearedFromOnSceneDateTime = isComplete ? received.AddHours(2 + (i % 3)) : null,
                RespondingClinicianTeam = teams[providerId - 1],
                Disposition = dispositions[i % dispositions.Length],
                ResponseLocation = locations[i % locations.Length],
                BHAppointmentOffered = i % 3 != 0,
                WhyNotBHAppointment = i % 3 == 0 ? "Not indicated in demo scenario" : null,
                OfferedWithin7Days = i % 2 == 0,
                AppointmentScheduled = i % 4 == 0,
                ScheduledWithin7Days = i % 5 != 0,
                WhyNotScheduled = i % 4 == 0 ? null : "Client chose to schedule independently",
                FollowUpOffered = i % 2 == 0,
                FiveDayFollowUpOffered = i % 3 == 0,
                IsComplete = isComplete,
                ProviderId = providerId
            };

            if (i % 2 == 0)
            {
                dispatch.FollowUps.Add(new FollowUp
                {
                    DateTime = received.AddDays(1),
                    Outcome = "Reached client; no additional immediate needs reported.",
                    Who = "Demo Clinician"
                });
            }

            if (i % 3 == 0)
            {
                dispatch.FiveDayFollowUp = new SelfDispatchFiveDayFollowUp
                {
                    DateTime = received.AddDays(5),
                    Outcome = "Five-day follow-up completed in demo data."
                };
            }

            db.SelfDispatches.Add(dispatch);
        }

        await db.SaveChangesAsync();

        var firstDispatch = await db.SelfDispatches.OrderBy(x => x.Id).FirstAsync();
        var history = new SelfDispatchAuditHistory
        {
            DispatchId = firstDispatch.Id,
            ChangedDate = now.AddDays(-1),
            ChangedBy = "Drew Internal Admin",
            Event = "Update"
        };
        db.SelfDispatchAuditHistories.Add(history);
        await db.SaveChangesAsync();

        db.SelfDispatchAuditFields.Add(new SelfDispatchAuditField
        {
            AuditHistoryId = history.Id,
            FieldName = nameof(SelfDispatch.Disposition),
            OldValue = "Referred",
            NewValue = firstDispatch.Disposition
        });

        await db.SaveChangesAsync();
    }
}
