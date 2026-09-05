using ColoradoDispatchPortal.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ColoradoDispatchPortal.Data;

public class DispatchPortalContext(DbContextOptions<DispatchPortalContext> options) : DbContext(options)
{
    public DbSet<SelfDispatch> SelfDispatches => Set<SelfDispatch>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<SelfDispatchFiveDayFollowUp> SelfDispatchFiveDayFollowUps => Set<SelfDispatchFiveDayFollowUp>();
    public DbSet<SelfDispatchAuditHistory> SelfDispatchAuditHistories => Set<SelfDispatchAuditHistory>();
    public DbSet<SelfDispatchAuditField> SelfDispatchAuditFields => Set<SelfDispatchAuditField>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<DemoUser> DemoUsers => Set<DemoUser>();
    public DbSet<UserProvider> UserProviders => Set<UserProvider>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProvider>().HasKey(x => new { x.DemoUserId, x.ProviderId });

        modelBuilder.Entity<UserProvider>()
            .HasOne(x => x.DemoUser)
            .WithMany(x => x.UserProviders)
            .HasForeignKey(x => x.DemoUserId);

        modelBuilder.Entity<UserProvider>()
            .HasOne(x => x.Provider)
            .WithMany(x => x.UserProviders)
            .HasForeignKey(x => x.ProviderId);

        modelBuilder.Entity<SelfDispatch>()
            .HasOne(x => x.Provider)
            .WithMany(x => x.SelfDispatches)
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FollowUp>()
            .HasOne(x => x.Dispatch)
            .WithMany(x => x.FollowUps)
            .HasForeignKey(x => x.DispatchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SelfDispatchFiveDayFollowUp>()
            .HasOne(x => x.Dispatch)
            .WithOne(x => x.FiveDayFollowUp)
            .HasForeignKey<SelfDispatchFiveDayFollowUp>(x => x.DispatchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SelfDispatchAuditHistory>()
            .HasOne(x => x.Dispatch)
            .WithMany(x => x.AuditHistories)
            .HasForeignKey(x => x.DispatchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SelfDispatchAuditField>()
            .HasOne(x => x.AuditHistory)
            .WithMany(x => x.Fields)
            .HasForeignKey(x => x.AuditHistoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SelfDispatch>()
            .HasIndex(x => x.ReferenceNumber)
            .IsUnique();

        modelBuilder.Entity<SelfDispatch>()
            .HasIndex(x => new { x.ProviderId, x.ReceivedDateTime });
    }
}
