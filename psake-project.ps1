Include "packages\Hangfire.Build.0.5.0\tools\psake-common.ps1"

Task Default -Depends Pack

Task Merge -Depends Compile -Description "Run ILRepack /internalize to merge required assemblies." {
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
    Exec { ls "tests\**\*.csproj" | % { dotnet build -c Release --no-restore --no-dependencies $_.FullName } }

    # We are running unit test project one by one, because pipelined version like the line above does not
    # support halting the whole execution pipeline when "dotnet test" command fails due to a failed test,
    # silently allowing build process to continue its execution even with failed tests.
    Exec { dotnet test -c Release --no-build "tests\Hangfire.Core.Tests" }
    Exec { dotnet test -c Release --no-build "tests\Hangfire.SqlServer.Tests" }
    Exec { dotnet test -c Release --no-build -p:TestTfmsInParallel=false "tests\Hangfire.SqlServer.Msmq.Tests" }
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
    
    Collect-Tool "src\Hangfire.SqlServer\DefaultInstall.sql"

    Collect-Localizations "Hangfire.Core" "net451"
    Collect-Localizations "Hangfire.Core" "net46"
    Collect-Localizations "Hangfire.Core" "netstandard1.3"
    Collect-Localizations "Hangfire.Core" "netstandard2.0"

    Collect-File "README.md"
    Collect-File "LICENSE.md"
    Collect-File "NOTICES"
    Collect-File "COPYING.LESSER"
    Collect-File "COPYING"
    Collect-File "LICENSE_STANDARD"
    Collect-File "LICENSE_ROYALTYFREE"
}

Task Pack -Depends Collect -Description "Create NuGet packages and archive files." {
    $version = Get-PackageVersion

    Create-Package "Hangfire" $version
    Create-Package "Hangfire.Core" $version
    Create-Package "Hangfire.SqlServer" $version
    Create-Package "Hangfire.SqlServer.Msmq" $version
    Create-Package "Hangfire.AspNetCore" $version
    Create-Package "Hangfire.NetCore" $version

    Create-Archive "Hangfire-$version"
}

Task Sign -Depends Pack -Description "Sign artifacts." {
    $version = Get-PackageVersion
    Sign-ArchiveContents "Hangfire-$version" "hangfire"
}
