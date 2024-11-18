using FolderMonitoringService;
using FolderMonitoringService.Interfaces;
using FolderMonitoringService.Services;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<FilesService>();
builder.Services.AddSingleton<IAppFileWatcher, AppFileWatcher>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService(opt =>
{
    opt.WaitForJobsToComplete = true;
});

var host = builder.Build();
await host.RunAsync();
