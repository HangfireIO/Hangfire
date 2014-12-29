Properties {
    $solution = "Hangfire.sln"
}

Include "packages\Hangfire.Build.0.1.3\tools\psake-common.ps1"

Task Default -Depends Collect

Task Test -Depends Compile -Description "Run unit and integration tests." {
    Run-XunitTests "Hangfire.Core.Tests"
    Run-XunitTests "Hangfire.SqlServer.Tests"
    Run-XunitTests "Hangfire.SqlServer.Msmq.Tests"
}

Task Merge -Depends Test -Description "Run ILMerge /internalize to merge assemblies." {
    # Remove `*.pdb` file to be able to prepare NuGet symbol packages.
    Remove-Item ((Get-SrcOutputDir "Hangfire.Core") + "\NCrontab.pdb")
    Remove-Item ((Get-SrcOutputDir "Hangfire.SqlServer") + "\Dapper.pdb")

    Merge-Assembly "Hangfire.Core" @("NCrontab", "CronExpressionDescriptor", "Microsoft.Owin")
    Merge-Assembly "Hangfire.SqlServer" @("Dapper")
}

Task Collect -Depends Merge -Description "Copy all artifacts to the build folder." {
    Collect-Assembly "Hangfire.Core" "Net45"
    Collect-Assembly "Hangfire.SqlServer" "Net45"
    Collect-Assembly "Hangfire.SqlServer.Msmq" "Net45"
    Collect-Assembly "Hangfire.SqlServer.RabbitMq" "Net45"

    Collect-Content "content\readme.txt"
    Collect-Tool "src\Hangfire.SqlServer\Install.sql"
}

Task Pack -Depends Collect -Description "Create NuGet packages and archive files." {
    $version = Get-BuildVersion

    Create-Archive "Hangfire-$version"

    Create-Package "Hangfire" $version
    Create-Package "Hangfire.Core" $version
    Create-Package "Hangfire.SqlServer" $version
    Create-Package "Hangfire.SqlServer.Msmq" $version
    Create-Package "Hangfire.SqlServer.RabbitMq" $version
}
