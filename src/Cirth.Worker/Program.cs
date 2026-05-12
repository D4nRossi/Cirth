using Cirth.Application;
using Cirth.Application.Common.Ports;
using Cirth.Infrastructure;
using Cirth.Worker;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/cirth-worker-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((_, cfg) =>
        cfg.ReadFrom.Configuration(builder.Configuration)
           .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    // Override the null ITenantProvider from Infrastructure with the job-scoped one.
    builder.Services.AddScoped<WorkerTenantProvider>();
    builder.Services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<WorkerTenantProvider>());
    builder.Services.AddHostedService<JobPollingService>();
    builder.Services.AddHostedService<StuckJobRecoveryService>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Cirth.Worker crashed on startup");
}
finally
{
    Log.CloseAndFlush();
}
