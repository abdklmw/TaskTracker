using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddControllersWithViews(options =>
{
    // Add global authorization filter with exclusions
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build()));
});

// Configure authentication to ensure login path and allow anonymous access to Identity pages
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
});

// Allow anonymous access to Identity pages
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AllowAnonymousIdentity", policy =>
        policy.Requirements.Add(new AllowAnonymousRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, AllowAnonymousHandler>();

// Register SetupService
builder.Services.AddScoped<SetupService>();

// Register PdfService
builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();

// Register RateCalculationService
builder.Services.AddScoped<RateCalculationService>();

// Register TimeEntryImportService
builder.Services.AddScoped<TimeEntryImportService>();

// Register DropdownService
builder.Services.AddScoped<DropdownService>();

// Register EmailService
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Override authorization for Identity routes
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    endpoints.MapRazorPages()
        .WithMetadata(new AllowAnonymousAttribute()); // Allow anonymous access to all Razor Pages (Identity)
});

app.Run();

// Custom authorization handler to allow anonymous access to specific routes
public class AllowAnonymousRequirement : IAuthorizationRequirement { }

public class AllowAnonymousHandler : AuthorizationHandler<AllowAnonymousRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AllowAnonymousRequirement requirement)
    {
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}