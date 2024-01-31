@echo off
.nuget\NuGet.exe restore .nuget\packages.config -OutputDirectory packages -UseLockFile -LockedMode -NoCache || exit /b 666
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command "& {Import-Module '.\packages\psake.*\tools\psake.psm1'; invoke-psake .\psake-project.ps1 %*; if ($psake.build_success -eq $false) { exit 1 } else { exit 0 }; }"
exit /B %errorlevel%
