# FolderMonitoringService

## Features
* The program monitors configured folders for changes to enforce the following policies 
	* allowed file types
	* maximum file size
* This a dotnet console application that can also be run as a background service using tools like nssm
* Initial scan of folder contents after startup of the program can also be enabled
* Subfolders monitoring can also be enabled

## Configuration
* The application can be configured using `appsettings.json` as follows
```json
{
  "Folders": [
    {
      "FolderPath": "C:\\Users\\Abcd\\Downloads\\FolderMonitorTest",
      "AllowedExtensions": [ "csv", "xlsx", "docx" ],
      "MaxFileSizeMb": 0.0001,
      "Enabled": true,
      "IncludeSubFolders": false,
      "InitialScan": true,
      "MaxAgeDays": 20,
      "AgeCheckCron": "0 * * * * ?"
    }
  ]
},
"Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Warning"
    }
  }
```
* Each folder to be configured should be specified as a list item in the `Folders` attribute of the `appsettings.json` file

## Publish the app
* While publishing with .NET CLI, following command can be used to create a self-contained executable file

```bash
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained false
```

* * While publishing with Visual Studio, edit the publish profile settings as follows
  * Profile Name - Folder profile
  * Configuration - Release | Any CPU
  * Deployment mode - Self-contained
  * Target runtime - win-x64
  * Produce single file

## Run the app as a background service with nssm
TODO
* Quartz schedule job from a controller - https://github.com/quartznet/quartznet/blob/main/src/Quartz.Examples.AspNetCore/Pages/Index.cshtml.cs
* https://www.quartz-scheduler.net/documentation/quartz-3.x/how-tos/one-off-job.html#dynamic-registration
* CRON syntax tutorial - https://crontab.guru/
* Attach data to job - https://stackoverflow.com/questions/46542950/quartz-net-how-to-send-instance-of-class-through-context