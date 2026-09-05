using ColoradoDispatchPortal.Data;
using ColoradoDispatchPortal.Data.Entities;
using ColoradoDispatchPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace ColoradoDispatchPortal.Services;

public class DemoAccessService(DispatchPortalContext db)
{
    private static readonly HashSet<string> ProviderScopedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "AGENCYSTAFF",
        "AGENCYADMIN",
        "AUDITOR"
    };

    public async Task<DemoUser> GetUserAsync(int userId) =>
        await db.DemoUsers
            .Include(x => x.UserProviders)
            .ThenInclude(x => x.Provider)
            .SingleAsync(x => x.Id == userId);

    public async Task<IReadOnlyList<DemoUserOption>> GetUsersAsync() =>
        await db.DemoUsers
            .OrderBy(x => x.Id)
            .Select(x => new DemoUserOption
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                Role = x.Role
            })
            .ToListAsync();

    public async Task<IReadOnlyList<int>?> GetProviderScopeAsync(int userId)
    {
        var user = await GetUserAsync(userId);
        if (!ProviderScopedRoles.Contains(user.Role))
        {
            return null;
        }

        return user.UserProviders.Select(x => x.ProviderId).Distinct().ToList();
    }

    public async Task<IReadOnlyList<Provider>> GetAccessibleProvidersAsync(int userId)
    {
        var scope = await GetProviderScopeAsync(userId);
        var query = db.Providers.AsNoTracking().OrderBy(x => x.Name).AsQueryable();
        if (scope is not null)
        {
            query = query.Where(x => scope.Contains(x.Id));
        }

        return await query.ToListAsync();
    }

    public async Task<string> GetAccessSummaryAsync(int userId)
    {
        var user = await GetUserAsync(userId);
        var scope = await GetProviderScopeAsync(userId);
        if (scope is null)
        {
            return "All demo providers";
        }

        var names = user.UserProviders.Select(x => x.Provider.Name).OrderBy(x => x).ToArray();
        return names.Length == 0 ? "No assigned providers" : string.Join(", ", names);
    }
}
