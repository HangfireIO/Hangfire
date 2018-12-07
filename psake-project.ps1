Framework 4.5.1
Include "packages\Hangfire.Build.0.2.6\tools\psake-common.ps1"

Task Default -Depends Collect
Task CI -Depends Pack

Task RestoreCore -Depends Restore, Clean {
    Exec { dotnet restore }
}

Task CompileCore -Depends RestoreCore {
    Exec { dotnet build -c Release }
}

Task Test -Depends CompileCore -Description "Run unit and integration tests." {
    Exec { dotnet test -c Release "tests\Hangfire.Core.Tests\Hangfire.Core.Tests.csproj" }
    Exec { dotnet test -c Release "tests\Hangfire.SqlServer.Tests\Hangfire.SqlServer.Tests.csproj" }
    Exec { dotnet test -c Release "tests\Hangfire.SqlServer.Msmq.Tests\Hangfire.SqlServer.Msmq.Tests.csproj" }
}

Task Merge -Depends Test -Description "Run ILRepack /internalize to merge assemblies." {
    Repack-Assembly @("Hangfire.Core", "net45") @("Cronos", "CronExpressionDescriptor", "Microsoft.Owin")
    Repack-Assembly @("Hangfire.SqlServer", "net45") @("Dapper")
    
    Repack-Assembly @("Hangfire.Core", "net46") @("Cronos", "CronExpressionDescriptor", "Microsoft.Owin")

    Publish-Assembly "Hangfire.Core" "netstandard1.3"
    Publish-Assembly "Hangfire.SqlServer" "netstandard1.3"
    
    Repack-Assembly @("Hangfire.Core", "netstandard1.3") @("Cronos")
    Repack-Assembly @("Hangfire.SqlServer", "netstandard1.3") @("Dapper")

    Publish-Assembly "Hangfire.Core" "netstandard2.0"
    Publish-Assembly "Hangfire.SqlServer" "netstandard2.0"
    
    Repack-Assembly @("Hangfire.Core", "netstandard2.0") @("Cronos")
    Repack-Assembly @("Hangfire.SqlServer", "netstandard2.0") @("Dapper")
}

Task Collect -Depends Merge -Description "Copy all artifacts to the build folder." {
    Collect-Assembly "Hangfire.Core" "net45"
    Collect-Assembly "Hangfire.SqlServer" "net45"
    Collect-Assembly "Hangfire.SqlServer.Msmq" "net45"
    Collect-Assembly "Hangfire.AspNetCore" "net451"

    Collect-Assembly "Hangfire.Core" "net46"

    Collect-Assembly "Hangfire.Core" "netstandard1.3"
    Collect-Assembly "Hangfire.SqlServer" "netstandard1.3"
    Collect-Assembly "Hangfire.AspNetCore" "netstandard1.3"
    
    Collect-Assembly "Hangfire.Core" "netstandard2.0"
    Collect-Assembly "Hangfire.SqlServer" "netstandard2.0"
    Collect-Assembly "Hangfire.AspNetCore" "netstandard2.0"
    
    Collect-Content "content\readme.txt"
    Collect-Tool "src\Hangfire.SqlServer\DefaultInstall.sql"

    Collect-Localizations "Hangfire.Core" "net45"
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
    
    Create-Package "Hangfire" $version
    Create-Package "Hangfire.Core" $version
    Create-Package "Hangfire.SqlServer" $version
    Create-Package "Hangfire.SqlServer.Msmq" $version
    Create-Package "Hangfire.AspNetCore" $version
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

function Publish-Assembly($project, $target) {
    $output = Get-SrcOutputDir $project $target
    Write-Host "Publishing '$project'/$target to '$output'..." -ForegroundColor "Green"
    Exec { dotnet publish --no-build -c Release -o $output -f $target "$base_dir\src\$project" }
    Remove-Item "$output\System.*"
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
