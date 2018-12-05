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

Task Test -Depends CompileCore -Description "Run unit and integration tests under OpenCover." {
    Exec { dotnet test -c Release "tests\Hangfire.Core.Tests\Hangfire.Core.Tests.csproj" }
    Exec { dotnet test -c Release "tests\Hangfire.SqlServer.Tests\Hangfire.SqlServer.Tests.csproj" }
    Exec { dotnet test -c Release "tests\Hangfire.SqlServer.Msmq.Tests\Hangfire.SqlServer.Msmq.Tests.csproj" }
}

Task Merge -Depends Test -Description "Run ILMerge /internalize to merge assemblies." {
    # Remove `*.pdb` file to be able to prepare NuGet symbol packages.
    Remove-File ((Get-SrcOutputDir "Hangfire.SqlServer") + "\Dapper.pdb")
    
    Merge-Assembly @("Hangfire.Core", "net45") @("Cronos", "CronExpressionDescriptor", "Microsoft.Owin")
    Merge-Assembly @("Hangfire.SqlServer", "net45") @("Dapper")
}

Task Collect -Depends Merge -Description "Copy all artifacts to the build folder." {
    Collect-Assembly "Hangfire.Core" "net45"
    Collect-Assembly "Hangfire.SqlServer" "net45"
    Collect-Assembly "Hangfire.SqlServer.Msmq" "net45"
    Collect-Assembly "Hangfire.AspNetCore" "net451"

    Collect-Assembly "Hangfire.Core" "netstandard1.3"
    Collect-Assembly "Hangfire.SqlServer" "netstandard1.3"
    Collect-Assembly "Hangfire.AspNetCore" "netstandard1.3"
    
    Collect-Content "content\readme.txt"
    Collect-Tool "src\Hangfire.SqlServer\DefaultInstall.sql"

    Collect-Localizations "Hangfire.Core" "net45"
    Collect-Localizations "Hangfire.Core" "netstandard1.3"

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
