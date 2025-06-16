using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog; // Add Serilog
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskTracker.Data;
using TaskTracker.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration) // Read from appsettings.json
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
    // Log the environment for debugging
    configuration.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} ({Environment}){NewLine}{Exception}");
});

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddControllersWithViews();

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
})
.AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
});
builder.Services.AddSingleton<IAuthorizationHandler, AllowAnonymousHandler>();

// Register SetupService
builder.Services.AddScoped<SetupService>();

// Register SettingsService
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Register ClientService
builder.Services.AddScoped<ClientService>();

// Register ProjectService
builder.Services.AddScoped<ProjectService>();

// Register ExpenseService
builder.Services.AddScoped<ExpenseService>();

// Register ProductService
builder.Services.AddScoped<ProductService>();

// Register InvoiceService
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

// Register TimeEntryService
builder.Services.AddScoped<TimeEntryService>();

// Register EmailService
builder.Services.AddScoped<IEmailService, EmailService>();

// Register UserService
builder.Services.AddScoped<IUserService, UserService>();

// Add Data Protection Configuration
builder.Services.AddDataProtection();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages()
    .WithMetadata(new AllowAnonymousAttribute()); // Allow anonymous access to all Razor Pages (Identity)

// Log application startup
app.Logger.LogInformation("Application started in {Environment} environment", app.Environment.EnvironmentName);

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