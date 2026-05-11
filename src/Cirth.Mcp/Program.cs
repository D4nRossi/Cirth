using Cirth.Application;
using Cirth.Application.Common.Ports;
using Cirth.Infrastructure;
using Cirth.Mcp.Auth;
using ModelContextProtocol.Server;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/cirth-mcp-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, _, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Application", "Cirth.Mcp"));

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddScoped<ITenantProvider, McpTenantProvider>();

    builder.Services
        .AddMcpServer()
        .WithToolsFromAssembly(typeof(Program).Assembly)
        .WithHttpTransport();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ApiKeyAuthMiddleware>();
    app.MapMcp();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Cirth.Mcp crashed on startup");
}
finally
{
    Log.CloseAndFlush();
}
