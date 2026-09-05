using ColoradoDispatchPortal.Models;

namespace ColoradoDispatchPortal.Repositories;

public interface ISelfDispatchRepo
{
    Task<PaginatedResponse<SelfDispatchModel>> GetSelfDispatches(
        int page,
        int pageSize,
        int userId,
        string search = "",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        List<FilterModel>? filters = null);

    Task<SelfDispatchModel?> GetSelfDispatchAsync(int dispatchId, int userId);
    Task<IReadOnlyList<DateTime>> GetDispatchDatesAsync(int userId);
    Task<IReadOnlyList<int>> GetProviderIds(int userId);
    Task<int> CreateDispatchAsync(SelfDispatchModel model, int userId);
    Task UpdateDispatchAsync(SelfDispatchModel model, int userId);
    Task DeleteDispatchAsync(int dispatchId, int userId);
    Task AddSelfDispatchHistoryAsync(SelfDispatchModel model, int userId, string eventName, int dispatchId, SelfDispatchModel? previousModel = null);
    Task<IReadOnlyList<SelfDispatchHistoryViewModel>> FindDispatchHistoryAsync(int dispatchId, int userId);
}
