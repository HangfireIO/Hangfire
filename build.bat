@echo off
.nuget\NuGet.exe install .nuget\packages.config -OutputDirectory packages
.nuget\NuGet.exe restore Hangfire.sln
powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "& {Import-Module '.\packages\psake.*\tools\psake.psm1'; invoke-psake .\psake-default.ps1 %1; if ($LastExitCode -ne 0) {write-host "ERROR: $LastExitCode" -fore RED; exit $lastexitcode} }"
