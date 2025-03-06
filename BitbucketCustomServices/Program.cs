using System.Reflection;
using System.Text.Json.Serialization;
using BitbucketCustomServices;
using BitbucketCustomServices.Endpoints.Auth;
using BitbucketCustomServices.Endpoints.Config;
using BitbucketCustomServices.Endpoints.Management;
using BitbucketCustomServices.Endpoints.Webhooks;
using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Ui.Core.Extensions;
using Serilog.Ui.SqliteDataProvider.Extensions;
using Serilog.Ui.Web.Extensions;
using Serilog.Ui.Web.Models;
using BitbucketCustomServices.Services;
using BitbucketCustomServices.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

var dbDirectory = Path.Combine(appPath, "wwwroot", "data");
if (!Directory.Exists(dbDirectory))
    Directory.CreateDirectory(dbDirectory);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("seed-config.json", optional: true)
    .AddEnvironmentVariables();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var sqliteDbPath = Path.Combine(dbDirectory, connectionString!);
var sqliteConnectionString = $"Data Source={sqliteDbPath}";

builder.Services
    .AddSingleton(builder.Configuration as IConfigurationRoot)
    .AddAntiforgery()
    .AddHttpClient()
    .AddLogging(logging => logging.AddConsole())
    .Configure<CookiePolicyOptions>(x =>
    {
        x.CheckConsentNeeded = _ => false;
        x.MinimumSameSitePolicy = SameSiteMode.None;
    })
    .AddSerilog(options => options
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.SQLite(
            sqliteDbPath: sqliteDbPath,
            retentionPeriod: TimeSpan.FromDays(7)));

builder.Services.AddSerilogUi(x => x.AddScopedAsyncAuthFilter<SerilogAuthFilter>()
    .UseSqliteServer(o =>
    {
        o.WithTable("Logs");
        o.WithConnectionString(sqliteConnectionString);
    }));
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnectionString));

builder.Services.AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromSeconds(2);
});

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(o =>
    {
        o.DefaultScheme = IdentityConstants.ApplicationScheme;
        o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies(o =>
    {
        o.ApplicationCookie?.Configure(options =>
        {
            options.LoginPath = new PathString("/login");
            options.LogoutPath = new PathString("/login");
            options.AccessDeniedPath = new PathString("/login/forbidden");
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(24);
        });
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddScoped<IBitbucketService, BitbucketService>();
builder.Services.AddScoped<IRepositoryService, RepositoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICascadeMergeService, CascadeMergeService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    await SeedData.InitializeAsync(roleManager, userManager, dbContext, configuration);
}

app.Use(async (context,
    requestDelegate) =>
{
    if (context.Request.Path.Value is not "/")
        await requestDelegate(context);
    else
        context.Response.Redirect("/management");
});

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.UseSerilogUi(options =>
{
    options
        .WithHomeUrl("/management")
        .WithRoutePrefix("logs")
        .WithAuthenticationType(AuthenticationType.Custom);
});


app.MapLoginEndpoints()
    .MapLogoutEndpoints()
    .MapUserRoleEndpoints()
    .MapManagementEndpoints()
    .MapManagementUsersEndpoints()
    .MapManagementUsersPageEndpoint()
    .MapConfigEditorEndpoint()
    .MapConfigSaveEndpoint()
    .MapWebhookCascadeMergeEndpoint()
    .MapWebhookTelegramNotificationEndpoint();

app.Run("http://0.0.0.0:7799");