using FolderMonitoringService;
using FolderMonitoringService.Interfaces;
using FolderMonitoringService.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IAppFileWatcher, AppFileWatcher>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
