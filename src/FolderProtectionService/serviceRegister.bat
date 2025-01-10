call nssm.exe install folder_protection_service "%cd%\FolderProtectionService.exe"
call nssm.exe set folder_protection_service AppStdout "%programdata%\FolderProtectionService\folder_protection_service.log"
call nssm.exe set folder_protection_service AppStderr "%programdata%\FolderProtectionService\folder_protection_service.log"
call nssm.exe set folder_protection_service AppStdoutCreationDisposition 4
call nssm.exe set folder_protection_service AppStderrCreationDisposition 4
call nssm.exe set folder_protection_service AppRotateFiles 1
call nssm.exe set folder_protection_service AppRotateOnline 1
call nssm.exe set folder_protection_service AppRotateBytes 1048576
call sc start folder_protection_service