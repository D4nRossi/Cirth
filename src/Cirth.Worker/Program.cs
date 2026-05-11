using Cirth.Application;
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
    builder.Services.AddHostedService<JobPollingService>();

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
