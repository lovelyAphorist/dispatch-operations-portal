using System.Text.Json;
using ColoradoDispatchPortal.Models;
using ColoradoDispatchPortal.Repositories;
using ColoradoDispatchPortal.Services;
using Microsoft.AspNetCore.Mvc;

namespace ColoradoDispatchPortal.Controllers;

[Route("Dispatches")]
public class DispatchController(
    ISelfDispatchRepo repo,
    DemoAccessService accessService,
    ILogger<DispatchController> logger) : Controller
{
    [HttpGet("")]
    [HttpGet("Dashboard")]
    public async Task<IActionResult> Dashboard(int demoUserId = 1)
    {
        var user = await accessService.GetUserAsync(demoUserId);
        var model = new DashboardViewModel
        {
            SelectedDemoUserId = demoUserId,
            SelectedDemoUserName = user.DisplayName,
            SelectedRole = user.Role,
            AccessSummary = await accessService.GetAccessSummaryAsync(demoUserId),
            DemoUsers = await accessService.GetUsersAsync()
        };

        return View(model);
    }

    // The route and method name intentionally mirror the recovered portal.
    [HttpGet("get-self-dispatches")]
    public async Task<IActionResult> GetPaginatedSelfDispatches(
        int page = 1,
        int pageSize = 10,
        string search = "",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string filters = "[]",
        int demoUserId = 1)
    {
        try
        {
            var parsedFilters = JsonSerializer.Deserialize<List<FilterModel>>(
                filters,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<FilterModel>();

            var result = await repo.GetSelfDispatches(
                page,
                pageSize,
                demoUserId,
                search,
                fromDate,
                toDate,
                parsedFilters);

            return Json(new
            {
                last_page = result.TotalPages,
                data = result.Items,
                success = true,
                total = result.TotalCount
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving paginated self dispatches for demo user {DemoUserId}", demoUserId);
            return StatusCode(500, new { success = false, message = "Unable to retrieve dispatches." });
        }
    }

    [HttpGet("EditDispatch")]
    public async Task<IActionResult> EditDispatch(int dispatchId, int demoUserId = 1)
    {
        var model = await repo.GetSelfDispatchAsync(dispatchId, demoUserId);
        if (model is null)
        {
            return NotFound();
        }

        await PopulateEditViewDataAsync(demoUserId, isCreate: false);
        return View(model);
    }

    [HttpGet("CreateDispatch")]
    public async Task<IActionResult> CreateDispatch(int demoUserId = 1)
    {
        var providers = await accessService.GetAccessibleProvidersAsync(demoUserId);
        var model = new SelfDispatchModel
        {
            ReceivedDateTime = DateTime.Now,
            ProviderId = providers.FirstOrDefault()?.Id ?? 0,
            ReferenceNumber = $"CO-DEMO-{DateTime.UtcNow:yyyyMMddHHmmss}"
        };

        await PopulateEditViewDataAsync(demoUserId, isCreate: true);
        return View("EditDispatch", model);
    }

    [HttpPost("CreateDispatch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDispatch(SelfDispatchModel model, int demoUserId = 1)
    {
        if (!ModelState.IsValid)
        {
            return Json(new
            {
                success = false,
                errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToArray()
            });
        }

        try
        {
            var id = await repo.CreateDispatchAsync(model, demoUserId);
            return Json(new { success = true, id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating self dispatch");
            return BadRequest(new { success = false, errors = new[] { ex.Message } });
        }
    }

    [HttpPost("UpdateDispatch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDispatch(SelfDispatchModel model, int demoUserId = 1)
    {
        if (!ModelState.IsValid)
        {
            return Json(new
            {
                success = false,
                errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToArray()
            });
        }

        try
        {
            await repo.UpdateDispatchAsync(model, demoUserId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating dispatch {DispatchId}", model.Id);
            return BadRequest(new { success = false, errors = new[] { ex.Message } });
        }
    }

    [HttpGet("GetDispatchHistory")]
    public async Task<IActionResult> GetDispatchHistory(int dispatchId, int demoUserId = 1)
    {
        var history = await repo.FindDispatchHistoryAsync(dispatchId, demoUserId);
        return Json(history);
    }

    // Compatibility-style alias reflecting the older history route discussed during development.
    [HttpPost("/SelfDispatch/History/{dispatchId:int}")]
    public async Task<IActionResult> History(int dispatchId, int demoUserId = 1)
    {
        var history = await repo.FindDispatchHistoryAsync(dispatchId, demoUserId);
        return Json(history);
    }

    [HttpPost("DeleteDispatch/{dispatchId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDispatch(int dispatchId, int demoUserId = 1)
    {
        try
        {
            await repo.DeleteDispatchAsync(dispatchId, demoUserId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting dispatch {DispatchId}", dispatchId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private async Task PopulateEditViewDataAsync(int demoUserId, bool isCreate)
    {
        ViewBag.DemoUserId = demoUserId;
        ViewBag.IsCreate = isCreate;
        ViewBag.Providers = await accessService.GetAccessibleProvidersAsync(demoUserId);
    }
}
