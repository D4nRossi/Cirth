using Cirth.Application;
using Cirth.Infrastructure;
using Cirth.Infrastructure.Auth;
using Cirth.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Identity.Web;
using MudBlazor.Services;
using Prometheus;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/cirth-web-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, _, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Application", "Cirth.Web"));

    // Auth — Entra ID OIDC
    builder.Services
        .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("EntraId"));

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("Admin", p => p.RequireRole("Admin"));
        options.AddPolicy("User", p => p.RequireAuthenticatedUser());
        options.FallbackPolicy = options.DefaultPolicy;
    });

    builder.Services.AddHttpContextAccessor();

    // Blazor + MudBlazor
    builder.Services.AddRazorComponents().AddInteractiveServerComponents();
    builder.Services.AddMudServices(cfg =>
    {
        cfg.SnackbarConfiguration.PositionClass = "bottom-right";
        cfg.SnackbarConfiguration.PreventDuplicates = false;
        cfg.SnackbarConfiguration.NewestOnTop = true;
    });

    // SignalR
    builder.Services.AddSignalR();

    // Application + Infrastructure
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // TenantProvider scoped to HTTP context
    builder.Services.AddScoped<Cirth.Application.Common.Ports.ITenantProvider, HttpTenantProvider>();

    // Rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = 429;
        options.AddFixedWindowLimiter("chat", o => { o.PermitLimit = 30; o.Window = TimeSpan.FromMinutes(1); });
        options.AddFixedWindowLimiter("search", o => { o.PermitLimit = 60; o.Window = TimeSpan.FromMinutes(1); });
        options.AddFixedWindowLimiter("upload", o => { o.PermitLimit = 5; o.Window = TimeSpan.FromMinutes(1); });
    });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!, name: "postgres", tags: ["ready"])
        .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis", tags: ["ready"])
        .AddUrlGroup(new Uri(builder.Configuration["Qdrant:HealthEndpoint"] ?? "http://localhost:6333/healthz"),
            name: "qdrant", tags: ["ready"]);

    var app = builder.Build();

    app.UseSerilogRequestLogging(opts =>
        opts.EnrichDiagnosticContext = (ctx, httpCtx) =>
            ctx.Set("CorrelationId", httpCtx.TraceIdentifier));

    if (!app.Environment.IsDevelopment())
        app.UseHsts();

    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Provision user after auth
    app.UseMiddleware<UserProvisioningMiddleware>();

    app.UseRateLimiter();
    app.UseAntiforgery();

    // Metrics
    app.UseHttpMetrics();
    app.MapMetrics("/metrics");

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new() { Predicate = h => h.Tags.Contains("ready") });

    app.MapHub<Cirth.Web.Hubs.CirthHub>("/hubs/cirth");
    app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

    // Sign-out endpoint
    app.MapGet("/signout", async ctx =>
    {
        await ctx.SignOutAsync();
        ctx.Response.Redirect("/");
    }).RequireAuthorization();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Cirth.Web crashed on startup");
}
finally
{
    Log.CloseAndFlush();
}
