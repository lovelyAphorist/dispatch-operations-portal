using System.Globalization;
using System.Reflection;
using AutoMapper;
using ColoradoDispatchPortal.Data;
using ColoradoDispatchPortal.Data.Entities;
using ColoradoDispatchPortal.Models;
using ColoradoDispatchPortal.Services;
using Microsoft.EntityFrameworkCore;

namespace ColoradoDispatchPortal.Repositories;

public class SelfDispatchRepo(
    DispatchPortalContext db,
    IMapper mapper,
    DemoAccessService accessService) : ISelfDispatchRepo
{
    private static readonly HashSet<string> NonAuditProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(SelfDispatchModel.Id),
        nameof(SelfDispatchModel.ProviderName),
        nameof(SelfDispatchModel.FollowUps)
    };

    public async Task<PaginatedResponse<SelfDispatchModel>> GetSelfDispatches(
        int page,
        int pageSize,
        int userId,
        string search = "",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        List<FilterModel>? filters = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);

        var query = db.SelfDispatches
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.FollowUps)
            .Include(x => x.FiveDayFollowUp)
            .AsQueryable();

        var providerScope = await accessService.GetProviderScopeAsync(userId);
        if (providerScope is not null)
        {
            if (providerScope.Count == 0)
            {
                return new PaginatedResponse<SelfDispatchModel>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    Items = Array.Empty<SelfDispatchModel>()
                };
            }

            query = query.Where(x => providerScope.Contains(x.ProviderId));
        }

        // Recovered portal behavior defaulted the dashboard to a recent window.
        var effectiveFrom = fromDate?.Date ?? DateTime.UtcNow.Date.AddMonths(-3);
        var effectiveToExclusive = (toDate?.Date ?? DateTime.UtcNow.Date).AddDays(1);
        query = query.Where(x =>
            x.DispatchDateTime.HasValue &&
            x.DispatchDateTime.Value >= effectiveFrom &&
            x.DispatchDateTime.Value < effectiveToExclusive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.ReferenceNumber.Contains(term) ||
                x.FirstName.Contains(term) ||
                x.LastName.Contains(term));
        }

        query = ApplyFilters(query, filters ?? new List<FilterModel>());

        var totalCount = await query.CountAsync();
        var entities = await query
            .OrderByDescending(x => x.ReceivedDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<SelfDispatchModel>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = mapper.Map<List<SelfDispatchModel>>(entities)
        };
    }

    public async Task<SelfDispatchModel?> GetSelfDispatchAsync(int dispatchId, int userId)
    {
        var query = db.SelfDispatches
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.FollowUps)
            .Include(x => x.FiveDayFollowUp)
            .Where(x => x.Id == dispatchId);

        query = await ApplyProviderScopeAsync(query, userId);
        var entity = await query.SingleOrDefaultAsync();
        return entity is null ? null : mapper.Map<SelfDispatchModel>(entity);
    }

    public async Task<IReadOnlyList<DateTime>> GetDispatchDatesAsync(int userId)
    {
        var query = db.SelfDispatches.AsNoTracking().AsQueryable();
        query = await ApplyProviderScopeAsync(query, userId);

        return await query
            .Where(x => x.DispatchDateTime.HasValue)
            .Select(x => x.DispatchDateTime!.Value.Date)
            .Distinct()
            .OrderByDescending(x => x)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<int>> GetProviderIds(int userId)
    {
        var scope = await accessService.GetProviderScopeAsync(userId);
        if (scope is null)
        {
            return await db.Providers.AsNoTracking().Select(x => x.Id).ToListAsync();
        }

        return scope;
    }

    public async Task<int> CreateDispatchAsync(SelfDispatchModel model, int userId)
    {
        await EnsureProviderAccessAsync(model.ProviderId, userId);

        var entity = mapper.Map<SelfDispatch>(model);
        entity.Id = 0;
        SyncFollowUps(entity, model);
        SyncFiveDayFollowUp(entity, model);

        db.SelfDispatches.Add(entity);
        await db.SaveChangesAsync();

        model.Id = entity.Id;
        await AddSelfDispatchHistoryAsync(model, userId, "Create", entity.Id);
        return entity.Id;
    }

    public async Task UpdateDispatchAsync(SelfDispatchModel model, int userId)
    {
        var query = db.SelfDispatches
            .Include(x => x.Provider)
            .Include(x => x.FollowUps)
            .Include(x => x.FiveDayFollowUp)
            .Where(x => x.Id == model.Id);
        query = await ApplyProviderScopeAsync(query, userId);

        var entity = await query.SingleOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Dispatch {model.Id} was not found or is outside the selected user's provider scope.");

        if (entity.IsComplete)
        {
            throw new InvalidOperationException("Completed dispatches are read-only.");
        }

        await EnsureProviderAccessAsync(model.ProviderId, userId);

        var previousModel = mapper.Map<SelfDispatchModel>(entity);
        mapper.Map(model, entity);
        SyncFollowUps(entity, model);
        SyncFiveDayFollowUp(entity, model);

        await db.SaveChangesAsync();
        await AddSelfDispatchHistoryAsync(model, userId, "Update", entity.Id, previousModel);
    }

    public async Task DeleteDispatchAsync(int dispatchId, int userId)
    {
        var query = db.SelfDispatches
            .Include(x => x.FollowUps)
            .Include(x => x.FiveDayFollowUp)
            .Include(x => x.AuditHistories)
            .ThenInclude(x => x.Fields)
            .Where(x => x.Id == dispatchId);
        query = await ApplyProviderScopeAsync(query, userId);

        var dispatch = await query.SingleOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Dispatch {dispatchId} was not found or is outside the selected user's provider scope.");

        // Explicit dependent cleanup preserves the foreign-key lesson from the
        // original portal instead of relying on database cascade behavior.
        var auditFields = dispatch.AuditHistories.SelectMany(x => x.Fields).ToList();
        if (auditFields.Count > 0)
        {
            db.SelfDispatchAuditFields.RemoveRange(auditFields);
        }

        if (dispatch.AuditHistories.Count > 0)
        {
            db.SelfDispatchAuditHistories.RemoveRange(dispatch.AuditHistories);
        }

        if (dispatch.FollowUps.Count > 0)
        {
            db.FollowUps.RemoveRange(dispatch.FollowUps);
        }

        if (dispatch.FiveDayFollowUp is not null)
        {
            db.SelfDispatchFiveDayFollowUps.Remove(dispatch.FiveDayFollowUp);
        }

        db.SelfDispatches.Remove(dispatch);
        await db.SaveChangesAsync();
    }

    public async Task AddSelfDispatchHistoryAsync(
        SelfDispatchModel model,
        int userId,
        string eventName,
        int dispatchId,
        SelfDispatchModel? previousModel = null)
    {
        var user = await accessService.GetUserAsync(userId);
        var newHistory = new SelfDispatchAuditHistory
        {
            DispatchId = dispatchId,
            ChangedDate = DateTime.UtcNow,
            ChangedBy = user.DisplayName,
            Event = eventName
        };

        db.SelfDispatchAuditHistories.Add(newHistory);
        await db.SaveChangesAsync(); // obtain auditId before adding field rows
        var auditId = newHistory.Id;

        if (eventName.Equals("Create", StringComparison.OrdinalIgnoreCase) || previousModel is null)
        {
            LogNewValues(model, auditId);
        }
        else
        {
            CompareAndLogChanges(previousModel, model, auditId);
        }

        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<SelfDispatchHistoryViewModel>> FindDispatchHistoryAsync(int dispatchId, int userId)
    {
        var dispatchQuery = db.SelfDispatches.AsNoTracking().Where(x => x.Id == dispatchId);
        dispatchQuery = await ApplyProviderScopeAsync(dispatchQuery, userId);
        if (!await dispatchQuery.AnyAsync())
        {
            return Array.Empty<SelfDispatchHistoryViewModel>();
        }

        return await db.SelfDispatchAuditHistories
            .AsNoTracking()
            .Where(x => x.DispatchId == dispatchId)
            .SelectMany(history => history.Fields.Select(field => new SelfDispatchHistoryViewModel
            {
                ChangedDate = history.ChangedDate,
                ChangedBy = history.ChangedBy,
                Event = history.Event,
                FieldName = field.FieldName,
                OldValue = field.OldValue,
                NewValue = field.NewValue
            }))
            .OrderByDescending(x => x.ChangedDate)
            .ThenBy(x => x.FieldName)
            .ToListAsync();
    }

    private static IQueryable<SelfDispatch> ApplyFilters(IQueryable<SelfDispatch> query, IEnumerable<FilterModel> filters)
    {
        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter.Value))
            {
                continue;
            }

            var value = filter.Value.Trim();
            query = filter.Field switch
            {
                nameof(SelfDispatchModel.ReferenceNumber) => query.Where(x => x.ReferenceNumber.Contains(value)),
                nameof(SelfDispatchModel.FirstName) => query.Where(x => x.FirstName.Contains(value)),
                nameof(SelfDispatchModel.LastName) => query.Where(x => x.LastName.Contains(value)),
                nameof(SelfDispatchModel.CancellationType) => query.Where(x => x.CancellationType != null && x.CancellationType.Contains(value)),
                nameof(SelfDispatchModel.RespondingClinicianTeam) => query.Where(x => x.RespondingClinicianTeam != null && x.RespondingClinicianTeam.Contains(value)),
                nameof(SelfDispatchModel.Disposition) => query.Where(x => x.Disposition != null && x.Disposition.Contains(value)),
                nameof(SelfDispatchModel.ResponseLocation) => query.Where(x => x.ResponseLocation != null && x.ResponseLocation.Contains(value)),
                _ => query
            };
        }

        return query;
    }

    private async Task<IQueryable<SelfDispatch>> ApplyProviderScopeAsync(IQueryable<SelfDispatch> query, int userId)
    {
        var providerScope = await accessService.GetProviderScopeAsync(userId);
        return providerScope is null ? query : query.Where(x => providerScope.Contains(x.ProviderId));
    }

    private async Task EnsureProviderAccessAsync(int providerId, int userId)
    {
        var providerScope = await accessService.GetProviderScopeAsync(userId);
        if (providerScope is not null && !providerScope.Contains(providerId))
        {
            throw new UnauthorizedAccessException("The selected demo user cannot access that provider.");
        }

        if (!await db.Providers.AnyAsync(x => x.Id == providerId))
        {
            throw new KeyNotFoundException($"Provider {providerId} does not exist.");
        }
    }

    private void SyncFollowUps(SelfDispatch entity, SelfDispatchModel model)
    {
        if (entity.FollowUps.Count > 0)
        {
            db.FollowUps.RemoveRange(entity.FollowUps);
            entity.FollowUps.Clear();
        }

        foreach (var followUp in model.FollowUps.Where(x => x.DateTime.HasValue || !string.IsNullOrWhiteSpace(x.Outcome) || !string.IsNullOrWhiteSpace(x.Who)))
        {
            entity.FollowUps.Add(new FollowUp
            {
                DateTime = followUp.DateTime,
                Outcome = followUp.Outcome,
                Who = followUp.Who
            });
        }
    }

    private void SyncFiveDayFollowUp(SelfDispatch entity, SelfDispatchModel model)
    {
        var hasFiveDayData = model.FiveDayFollowUpDateTime.HasValue || !string.IsNullOrWhiteSpace(model.FiveDayFollowUpOutcome);
        if (!hasFiveDayData)
        {
            if (entity.FiveDayFollowUp is not null)
            {
                db.SelfDispatchFiveDayFollowUps.Remove(entity.FiveDayFollowUp);
                entity.FiveDayFollowUp = null;
            }
            return;
        }

        entity.FiveDayFollowUp ??= new SelfDispatchFiveDayFollowUp();
        entity.FiveDayFollowUp.DateTime = model.FiveDayFollowUpDateTime;
        entity.FiveDayFollowUp.Outcome = model.FiveDayFollowUpOutcome;
    }

    private void CompareAndLogChanges(SelfDispatchModel oldModel, SelfDispatchModel newModel, int auditId)
    {
        foreach (var property in GetAuditableProperties())
        {
            var oldValue = GetValueAsString(property.GetValue(oldModel));
            var newValue = GetValueAsString(property.GetValue(newModel));
            if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                continue;
            }

            db.SelfDispatchAuditFields.Add(new SelfDispatchAuditField
            {
                AuditHistoryId = auditId,
                FieldName = property.Name,
                OldValue = oldValue,
                NewValue = newValue
            });
        }
    }

    private void LogNewValues(SelfDispatchModel model, int auditId)
    {
        foreach (var property in GetAuditableProperties())
        {
            var value = GetValueAsString(property.GetValue(model));
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            db.SelfDispatchAuditFields.Add(new SelfDispatchAuditField
            {
                AuditHistoryId = auditId,
                FieldName = property.Name,
                OldValue = null,
                NewValue = value
            });
        }
    }

    private static IEnumerable<PropertyInfo> GetAuditableProperties() =>
        typeof(SelfDispatchModel)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.CanRead && !NonAuditProperties.Contains(x.Name))
            .Where(x => x.PropertyType == typeof(string)
                        || x.PropertyType == typeof(int)
                        || x.PropertyType == typeof(bool)
                        || x.PropertyType == typeof(DateTime?)
                        || x.PropertyType == typeof(DateTime));

    private static string? GetValueAsString(object? value) => value switch
    {
        null => null,
        DateTime dateTime => dateTime.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
        bool boolean => boolean ? "True" : "False",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)
    };
}
