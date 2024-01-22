Framework 4.5.1
Include "packages\Hangfire.Build.0.2.6\tools\psake-common.ps1"

Task Default -Depends Collect
Task CI -Depends Pack

Task Build -Depends Clean -Description "Restore all the packages and build the whole solution." {
    Exec { dotnet build -c Release }
}

Task Merge -Depends Build -Description "Run ILRepack /internalize to merge required assemblies." {
    Repack-Assembly @("Hangfire.Core", "net451") @("Cronos", "CronExpressionDescriptor", "Microsoft.Owin")
    Repack-Assembly @("Hangfire.Core", "net46") @("Cronos", "CronExpressionDescriptor", "Microsoft.Owin")
    Repack-Assembly @("Hangfire.SqlServer", "net451") @("Dapper")

    Repack-Assembly @("Hangfire.Core", "netstandard1.3") @("Cronos")
    Repack-Assembly @("Hangfire.Core", "netstandard2.0") @("Cronos")
    Repack-Assembly @("Hangfire.SqlServer", "netstandard1.3") @("Dapper")
    Repack-Assembly @("Hangfire.SqlServer", "netstandard2.0") @("Dapper")
}

Task Test -Depends Merge -Description "Run unit and integration tests against merged assemblies." {
    # Dependencies shouldn't be re-built, because we need to run tests against merged assemblies to test
    # the same assemblies that are distributed to users. Since the `dotnet test` command doesn't support
    # the `--no-dependencies` command directly, we need to re-build tests themselves first.
    Exec { ls "tests\**\*.csproj" | % { dotnet build -c Release --no-dependencies $_.FullName } }

    # We are running unit test project one by one, because pipelined version like the line above does not
    # support halting the whole execution pipeline when "dotnet test" command fails due to a failed test,
    # silently allowing build process to continue its execution even with failed tests.
    Exec { dotnet test -c Release --no-build "tests\Hangfire.Core.Tests" }
    Exec { dotnet test -c Release --no-build "tests\Hangfire.SqlServer.Tests" }
    Exec { dotnet test -c Release --no-build "tests\Hangfire.SqlServer.Msmq.Tests" }
}

Task Collect -Depends Test -Description "Copy all artifacts to the build folder." {
    Collect-Assembly "Hangfire.Core" "net451"
    Collect-Assembly "Hangfire.SqlServer" "net451"
    Collect-Assembly "Hangfire.SqlServer.Msmq" "net451"
    Collect-Assembly "Hangfire.NetCore" "net451"
    Collect-Assembly "Hangfire.AspNetCore" "net451"

    Collect-Assembly "Hangfire.Core" "net46"

    Collect-Assembly "Hangfire.Core" "netstandard1.3"
    Collect-Assembly "Hangfire.SqlServer" "netstandard1.3"
    Collect-Assembly "Hangfire.NetCore" "netstandard1.3"
    Collect-Assembly "Hangfire.AspNetCore" "netstandard1.3"
    
    Collect-Assembly "Hangfire.Core" "netstandard2.0"
    Collect-Assembly "Hangfire.SqlServer" "netstandard2.0"
    Collect-Assembly "Hangfire.AspNetCore" "netstandard2.0"
    Collect-Assembly "Hangfire.NetCore" "netstandard2.0"

    Collect-Assembly "Hangfire.NetCore" "net461"
    Collect-Assembly "Hangfire.AspNetCore" "net461"

    Collect-Assembly "Hangfire.AspNetCore" "netcoreapp3.0"
    Collect-Assembly "Hangfire.NetCore" "netstandard2.1"
    
    Collect-Content "content\readme.txt"
    Collect-Tool "src\Hangfire.SqlServer\DefaultInstall.sql"

    Collect-Localizations "Hangfire.Core" "net451"
    Collect-Localizations "Hangfire.Core" "net46"
    Collect-Localizations "Hangfire.Core" "netstandard1.3"
    Collect-Localizations "Hangfire.Core" "netstandard2.0"

    Collect-File "LICENSE.md"
    Collect-File "NOTICES"
    Collect-File "COPYING.LESSER"
    Collect-File "COPYING"
    Collect-File "LICENSE_STANDARD"
    Collect-File "LICENSE_ROYALTYFREE"
}

Task Pack -Depends Collect -Description "Create NuGet packages and archive files." {
    $version = Get-PackageVersion

    Create-Archive "Hangfire-$version"
    
    Create-Package2 "Hangfire" $version
    Create-Package2 "Hangfire.Core" $version
    Create-Package2 "Hangfire.SqlServer" $version
    Create-Package2 "Hangfire.SqlServer.Msmq" $version
    Create-Package2 "Hangfire.AspNetCore" $version
    Create-Package2 "Hangfire.NetCore" $version
}

function Collect-Localizations($project, $target) {
    Write-Host "Collecting localizations for '$target/$project'..." -ForegroundColor "Green"
    
    $output = (Get-SrcOutputDir $project $target)
    $dirs = Get-ChildItem -Path $output -Directory

    foreach ($dir in $dirs) {
        $source = "$output\$dir\$project.resources.dll"

        if (Test-Path $source) {
            Write-Host "  Collecting '$dir' localization..."

            $destination = "$build_dir\$target\$dir"

            Create-Directory $destination
            Copy-Files $source $destination
        }
    }
}

function Repack-Assembly($projectWithOptionalTarget, $internalizeAssemblies, $target) {
    $project = $projectWithOptionalTarget
    $target = $null

    $base_dir = resolve-path .
    $ilrepack = "$base_dir\packages\ilrepack.*\tools\ilrepack.exe"

    if ($projectWithOptionalTarget -Is [System.Array]) {
        $project = $projectWithOptionalTarget[0]
        $target = $projectWithOptionalTarget[1]
    }

    Write-Host "Merging '$project'/$target with $internalizeAssemblies..." -ForegroundColor "Green"

    $internalizePaths = @()

    $projectOutput = Get-SrcOutputDir $project $target

    foreach ($assembly in $internalizeAssemblies) {
        $internalizePaths += "$assembly.dll"
    }

    $primaryAssemblyPath = "$project.dll"
    $temp_dir = "$base_dir\temp"

    Create-Directory $temp_dir

    Push-Location
    Set-Location -Path $projectOutput

    Exec { .$ilrepack `
        /out:"$temp_dir\$project.dll" `
        /target:library `
        /internalize `
        $primaryAssemblyPath `
        $internalizePaths `
    }

    Pop-Location

    Move-Files "$temp_dir\$project.*" $projectOutput
}

function Create-Package2($project, $version) {
    Write-Host "Creating NuGet package for '$project'..." -ForegroundColor "Green"

    Create-Directory $temp_dir
    Copy-Files "$nuspec_dir\$project.nuspec" $temp_dir

    $commit = (git rev-parse HEAD)

    Try {
        Write-Host "Patching version with '$version'..." -ForegroundColor "DarkGray"
        Replace-Content "$nuspec_dir\$project.nuspec" '%version%' $version
        Write-Host "Patching commit hash with '$commit'..." -ForegroundColor "DarkGray"
        Replace-Content "$nuspec_dir\$project.nuspec" '%commit%' $commit
        Exec { .$nuget pack "$nuspec_dir\$project.nuspec" -OutputDirectory "$build_dir" -BasePath "$build_dir" -Version "$version" }
    }
    Finally {
        Move-Files "$temp_dir\$project.nuspec" $nuspec_dir
    }
}