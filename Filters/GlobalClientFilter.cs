using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TaskTracker.Services;

namespace TaskTracker.Filters
{
    public class GlobalClientFilter : IAsyncActionFilter
    {
        private readonly ClientService _clientService;

        public GlobalClientFilter(ClientService clientService)
        {
            _clientService = clientService;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            if (context.Controller is Controller controller &&
                context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                int globalClientId = context.HttpContext.Session.GetInt32("GlobalClientId") ?? 0;
                var clientDropdown = await _clientService.GetClientDropdownAsync(globalClientId);

                controller.ViewData["GlobalClientId"] = globalClientId;
                controller.ViewData["GlobalClientDropdown"] = clientDropdown;

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
