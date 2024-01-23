@echo off
.nuget\NuGet.exe restore .nuget\packages.config -OutputDirectory packages -UseLockFile -LockedMode -NoCache || exit /b 666
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command "& {Import-Module '.\packages\psake.*\tools\psake.psm1'; invoke-psake .\psake-project.ps1 %*; if ($LastExitCode -and $LastExitCode -ne 0) {write-host "ERROR CODE: $LastExitCode" -fore RED; exit $lastexitcode} }"
