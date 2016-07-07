Framework 4.5.1
Include "packages\Hangfire.Build.0.2.4\tools\psake-common.ps1"

Properties {
    $solution = "Hangfire.sln"
    $coverage_file = "coverage.xml"
    $coverage_filter = "+[Hangfire.*]* -[*.Tests]* -[*]*.Annotations.* -[*]*.Dashboard.* -[*]*.Logging.* -[*]*.ExpressionUtil.*"
}

Task Default -Depends Collect

Task Test -Depends Compile -Description "Run unit and integration tests under OpenCover." {
	Remove-File $coverage_file
    
    Run-OpenCover "Hangfire.Core.Tests" $coverage_file $coverage_filter
    Run-OpenCover "Hangfire.SqlServer.Tests" $coverage_file $coverage_filter
    Run-OpenCover "Hangfire.SqlServer.Msmq.Tests" $coverage_file $coverage_filter
}

Task Merge -Depends Test -Description "Run ILMerge /internalize to merge assemblies." {
    # Remove `*.pdb` file to be able to prepare NuGet symbol packages.
	Remove-File ((Get-SrcOutputDir "Hangfire.SqlServer") + "\Dapper.pdb")
    
    Merge-Assembly "Hangfire.Core" @("NCrontab", "CronExpressionDescriptor", "Microsoft.Owin")
    Merge-Assembly "Hangfire.SqlServer" @("Dapper")
}

Task Collect -Depends Merge -Description "Copy all artifacts to the build folder." {
    Collect-Assembly "Hangfire.Core" "net45"
    Collect-Assembly "Hangfire.SqlServer" "net45"
    Collect-Assembly "Hangfire.SqlServer.Msmq" "net45"

	Collect-Assembly "Hangfire.Core" "netstandard1.3"
	Collect-Assembly "Hangfire.SqlServer" "netstandard1.3"
	Collect-Assembly "Hangfire.AspNetCore" "netstandard1.3"
    
    Collect-Content "content\readme.txt"
    Collect-Tool "src\Hangfire.SqlServer\DefaultInstall.sql"
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
