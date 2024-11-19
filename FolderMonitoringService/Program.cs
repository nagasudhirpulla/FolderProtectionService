using FolderMonitoringService;
using FolderMonitoringService.Services;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService(opt =>
{
    opt.WaitForJobsToComplete = true;
});

builder.Services.AddSingleton<FilesService>();
builder.Services.AddSingleton<FolderConfigsService>();

builder.Services.AddSingleton<FileChangeWatcher>();
builder.Services.AddSingleton<FileAgeWatcher>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
