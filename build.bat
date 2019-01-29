@echo off
.nuget\NuGet.exe install .nuget\packages.config -OutputDirectory packages -Verbosity normal
powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "& {Import-Module '.\packages\psake.*\tools\psake.psm1'; invoke-psake .\psake-project.ps1 %*; if ($psake.build_success -eq $false) { write-host "ERROR, build/test failure" -fore RED;  exit 1 } else { if ($LastExitCode -and $LastExitCode -ne 0) { write-host "ERROR CODE: $LastExitCode" -fore RED; exit $lastexitcode } } }"
