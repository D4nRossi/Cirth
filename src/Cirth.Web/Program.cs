using Cirth.Application;
using Cirth.Infrastructure;
using Cirth.Infrastructure.Auth;
using Cirth.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
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

    // Data Protection com chaves persistidas em disco — sem isso cada restart do processo
    // (inclusive hot reload) gera novas chaves e invalida os cookies de correlação OIDC em voo.
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(
            Path.Combine(builder.Environment.ContentRootPath, ".data-protection-keys")));

    // Auth — Entra ID OIDC
    builder.Services
        .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("EntraId"));

    // PostConfigure roda DEPOIS dos IPostConfigureOptions do Microsoft.Identity.Web.
    // ResponseType=code: authorization code flow (PKCE) — não requer implicit grant no Azure.
    // O padrão do AddMicrosoftIdentityWebApp é id_token (implicit) que causa AADSTS700054.
    // SameSite=None: Azure faz POST de volta para /signin-oidc (cross-site) — Lax bloquearia
    // os cookies de correlação nesse POST.
    builder.Services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.ResponseType = "code";
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.NonceCookie.SameSite = SameSiteMode.None;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
    });

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
    builder.Services.AddSignalRNotifications();

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

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();

    // Must run before UseAuthorization so role claims are present when policies are evaluated.
    app.UseMiddleware<UserProvisioningMiddleware>();

    app.UseAuthorization();

    app.UseRateLimiter();
    app.UseAntiforgery();

    // Metrics
    app.UseHttpMetrics();
    app.MapMetrics("/metrics");

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new() { Predicate = h => h.Tags.Contains("ready") });

    app.MapHub<Cirth.Infrastructure.Auth.CirthHub>("/hubs/cirth");
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
