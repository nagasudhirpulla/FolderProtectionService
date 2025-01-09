# FolderProtectionService

## Features
* The program monitors configured folders for changes to enforce the following policies 
	* allowed file types
	* maximum file size
    * maximum file age (checked periodically)
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
      "FolderCheckCron": "0 * * * * ?"
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

## CRON syntax for quartz jobs
A cron expression is a string comprised of 6 or 7 fields separated by white space. 
Fields can contain any of the allowed values, along with various combinations of the allowed special characters for that field. The fields are as follows:

| **Field Name**   | **Mandatory** | **Allowed Values** | **Allowed Special Characters** |
|------------------|---------------|--------------------|--------------------------------|
| **Seconds**      | YES           | 0\-59              | , \- \* /                      |
| **Minutes**      | YES           | 0\-59              | , \- \* /                      |
| **Hours**        | YES           | 0\-23              | , \- \* /                      |
| **Day of month** | YES           | 1\-31              | , \- \* ? / L W                |
| **Month**        | YES           | 1\-12 or JAN\-DEC  | , \- \* /                      |
| **Day of week**  | YES           | 1\-7 or SUN\-SAT   | , \- \* ? / L \#               |
| **Year**         | NO            | empty, 1970\-2099  | , \- \* /                      |


 
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

## References
* Quartz Cron syntax docs - https://www.quartz-scheduler.net/documentation/quartz-3.x/how-tos/crontrigger.html#format
* General CRON syntax tutorial - https://crontab.guru/
* Quartz schedule job from a controller - https://github.com/quartznet/quartznet/blob/main/src/Quartz.Examples.AspNetCore/Pages/Index.cshtml.cs
* https://www.quartz-scheduler.net/documentation/quartz-3.x/how-tos/one-off-job.html#dynamic-registration
* Attach data to job - https://stackoverflow.com/questions/46542950/quartz-net-how-to-send-instance-of-class-through-context

## TODOs
* Explore DirectoryScanJob https://www.quartz-scheduler.net/documentation/quartz-3.x/packages/quartz-jobs.html#directoryscanjob
* Enable Spell checker