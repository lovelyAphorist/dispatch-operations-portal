using AutoMapper;
using ColoradoDispatchPortal.Data;
using ColoradoDispatchPortal.Data.Entities;
using ColoradoDispatchPortal.Mapping;
using ColoradoDispatchPortal.Models;
using ColoradoDispatchPortal.Repositories;
using ColoradoDispatchPortal.Services;
using Microsoft.EntityFrameworkCore;

namespace ColoradoDispatchPortal.Tests;

public class SelfDispatchRepoTests
{
    [Fact]
    public async Task GetSelfDispatches_AgencyStaff_OnlyReturnsAssignedProvider()
    {
        await using var db = CreateContext();
        SeedAccess(db);
        db.SelfDispatches.AddRange(
            DemoDispatch("CO-TEST-1", 1),
            DemoDispatch("CO-TEST-2", 2));
        await db.SaveChangesAsync();

        var repo = CreateRepo(db);
        var result = await repo.GetSelfDispatches(1, 10, 2, fromDate: DateTime.UtcNow.AddDays(-5), toDate: DateTime.UtcNow.AddDays(1));

        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].ProviderId);
    }

    [Fact]
    public async Task UpdateDispatchAsync_WritesFieldLevelAuditHistory()
    {
        await using var db = CreateContext();
        SeedAccess(db);
        var entity = DemoDispatch("CO-TEST-3", 1);
        entity.IsComplete = false;
        db.SelfDispatches.Add(entity);
        await db.SaveChangesAsync();

        var repo = CreateRepo(db);
        var model = (await repo.GetSelfDispatchAsync(entity.Id, 2))!;
        model.Disposition = "Updated disposition";

        await repo.UpdateDispatchAsync(model, 2);
        var history = await repo.FindDispatchHistoryAsync(entity.Id, 2);

        Assert.Contains(history, x => x.FieldName == nameof(SelfDispatchModel.Disposition) && x.NewValue == "Updated disposition");
    }

    [Fact]
    public async Task DeleteDispatchAsync_RemovesDependentRowsBeforeDispatch()
    {
        await using var db = CreateContext();
        SeedAccess(db);
        var entity = DemoDispatch("CO-TEST-4", 1);
        entity.FollowUps.Add(new FollowUp { Who = "Test", Outcome = "Reached", DateTime = DateTime.UtcNow });
        entity.FiveDayFollowUp = new SelfDispatchFiveDayFollowUp { DateTime = DateTime.UtcNow, Outcome = "Done" };
        db.SelfDispatches.Add(entity);
        await db.SaveChangesAsync();

        var history = new SelfDispatchAuditHistory
        {
            DispatchId = entity.Id,
            ChangedBy = "Test",
            ChangedDate = DateTime.UtcNow,
            Event = "Update"
        };
        db.SelfDispatchAuditHistories.Add(history);
        await db.SaveChangesAsync();
        db.SelfDispatchAuditFields.Add(new SelfDispatchAuditField { AuditHistoryId = history.Id, FieldName = "Disposition", OldValue = "A", NewValue = "B" });
        await db.SaveChangesAsync();

        var repo = CreateRepo(db);
        await repo.DeleteDispatchAsync(entity.Id, 2);

        Assert.False(await db.SelfDispatches.AnyAsync(x => x.Id == entity.Id));
        Assert.False(await db.FollowUps.AnyAsync(x => x.DispatchId == entity.Id));
        Assert.False(await db.SelfDispatchFiveDayFollowUps.AnyAsync(x => x.DispatchId == entity.Id));
        Assert.False(await db.SelfDispatchAuditHistories.AnyAsync(x => x.DispatchId == entity.Id));
        Assert.False(await db.SelfDispatchAuditFields.AnyAsync());
    }

    private static DispatchPortalContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DispatchPortalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DispatchPortalContext(options);
    }

    private static void SeedAccess(DispatchPortalContext db)
    {
        db.Providers.AddRange(
            new Provider { Id = 1, Name = "Provider One" },
            new Provider { Id = 2, Name = "Provider Two" });
        db.DemoUsers.AddRange(
            new DemoUser { Id = 2, DisplayName = "Agency User", Role = "AGENCYSTAFF" },
            new DemoUser { Id = 4, DisplayName = "Internal User", Role = "INTERNALADMIN" });
        db.UserProviders.Add(new UserProvider { DemoUserId = 2, ProviderId = 1 });
        db.SaveChanges();
    }

    private static SelfDispatch DemoDispatch(string referenceNumber, int providerId) => new()
    {
        ReferenceNumber = referenceNumber,
        FirstName = "Demo",
        LastName = "Person",
        ProviderId = providerId,
        ReceivedDateTime = DateTime.UtcNow.AddHours(-2),
        DispatchDateTime = DateTime.UtcNow.AddHours(-1),
        Disposition = "Initial",
        IsComplete = false
    };

    private static SelfDispatchRepo CreateRepo(DispatchPortalContext db)
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var access = new DemoAccessService(db);
        return new SelfDispatchRepo(db, mapper, access);
    }
}
