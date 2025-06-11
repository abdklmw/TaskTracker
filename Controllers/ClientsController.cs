using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Data;
using TaskTracker.Models.Client;
using TaskTracker.Services;

public class ClientsController : Controller
{
    private readonly ClientService _clientService;
    private readonly SetupService _setupService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClientsController(
        ClientService clientService,
        SetupService setupService,
        UserManager<ApplicationUser> userManager)
    {
        _clientService = clientService;
        _setupService = setupService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        bool userIsAuthenticated = User?.Identity?.IsAuthenticated ?? false;

        if (User == null || !userIsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = _userManager.GetUserId(User);

        if (userIsAuthenticated)
        {
            var setupResult = await _setupService.CheckSetupAsync(userId);
            if (setupResult != null)
            {
                return setupResult;
            }
        }

        return View(await _clientService.GetAllClientsAsync());
    }

    public IActionResult Create()
    {
        ViewBag.VisibleCreateForm = true;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,AccountsReceivableName,Email,Phone,Address,DefaultRate")] Client client)
    {
        if (ModelState.IsValid)
        {
            var (success, error) = await _clientService.CreateClientAsync(client);
            if (success)
            {
                TempData["SuccessMessage"] = "Client created successfully.";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = error;
        }
        else
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = string.Join("; ", errors);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ClientID,Name,AccountsReceivableName,Email,Phone,Address,DefaultRate")] Client client)
    {
        if (id != client.ClientID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var (success, error) = await _clientService.UpdateClientAsync(client);
            if (success)
            {
                TempData["SuccessMessage"] = "Client updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = error ?? "Client not found.";
        }
        else
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = string.Join("; ", errors);
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var client = await _clientService.GetClientByIdAsync(id.Value);
        if (client == null)
        {
            return NotFound();
        }

        return View(client);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _clientService.DeleteClientAsync(id);
        return RedirectToAction(nameof(Index));
    }
}