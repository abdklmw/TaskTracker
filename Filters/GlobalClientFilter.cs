using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TaskTracker.Services;

namespace TaskTracker.Filters
{
    public class GlobalClientFilter : IAsyncActionFilter
    {
        private readonly ClientService _clientService;
        private readonly IUserPreferenceService _preferenceService;

        public GlobalClientFilter(
            ClientService clientService,
            IUserPreferenceService preferenceService)
        {
            _clientService = clientService;
            _preferenceService = preferenceService;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            if (context.Controller is Controller controller &&
                context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                string userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

                int globalClientId = 0;
                int invoicesRecordLimit = 10;
                int timeEntriesRecordLimit = 10;
                int expensesRecordLimit = 10;

                if (!string.IsNullOrEmpty(userId))
                {
                    globalClientId = await _preferenceService.GetPreferredClientIdAsync(userId);
                    invoicesRecordLimit = await _preferenceService.GetRecordLimitAsync(userId, "invoices");
                    timeEntriesRecordLimit = await _preferenceService.GetRecordLimitAsync(userId, "timeentries");
                    expensesRecordLimit = await _preferenceService.GetRecordLimitAsync(userId, "expenses");
                }

                var clientDropdown = await _clientService.GetClientDropdownAsync(globalClientId);

                controller.ViewData["GlobalClientId"] = globalClientId;
                controller.ViewData["GlobalClientDropdown"] = clientDropdown;
                controller.ViewData["UserInvoicesRecordLimit"] = invoicesRecordLimit;
                controller.ViewData["UserTimeEntriesRecordLimit"] = timeEntriesRecordLimit;
                controller.ViewData["UserExpensesRecordLimit"] = expensesRecordLimit;

                if (globalClientId != 0)
                {
                    var selectedItem = clientDropdown.FirstOrDefault(c => c.Value == globalClientId.ToString());
                    controller.ViewData["GlobalClientName"] = selectedItem?.Text ?? "Unknown";
                }
            }

            await next();
        }
    }
}
