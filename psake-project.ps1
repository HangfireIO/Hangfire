Framework 4.5.1
Properties {
    $solution = "Hangfire.sln"
    $opencover = "packages\OpenCover.*\opencover.console.exe"
    $coverage_file = "coverage.xml"
    $coverage_filter = "+[Hangfire.*]* -[*.Tests]* -[*]*.Annotations.* -[*]*.Dashboard.* -[*]*.Logging.* -[*]*.ExpressionUtil.*"
}

Include "packages\Hangfire.Build.0.1.3\tools\psake-common.ps1"

Task Default -Depends Collect

Task Test -Depends Compile -Description "Run unit and integration tests under OpenCover." {
    If (Test-Path $coverage_file) {
        "Removing '$coverage_file'..."
        Remove-Item $coverage_file -Force
    }
    
    Run-OpenCover "Hangfire.Core.Tests"
    Run-OpenCover "Hangfire.SqlServer.Tests"
    Run-OpenCover "Hangfire.SqlServer.Msmq.Tests"
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
    Collect-Tool "src\Hangfire.SqlServer\DefaultInstall.sql"
}

Task Pack -Depends Collect -Description "Create NuGet packages and archive files." {
    $version = Get-BuildVersion

    $tag = $env:APPVEYOR_REPO_TAG_NAME
    if ($tag -And $tag.StartsWith("v$version-")) {
        "Using tag-based version for packages."
        $version = $tag.Substring(1)
    }
    
    Create-Archive "Hangfire-$version"
    
    Create-Package "Hangfire" $version
    Create-Package "Hangfire.Core" $version
    Create-Package "Hangfire.SqlServer" $version
    Create-Package "Hangfire.SqlServer.Msmq" $version
    Create-Package "Hangfire.SqlServer.RabbitMq" $version
}

function Run-OpenCover($assembly) {
    Exec {
        if ($env:APPVEYOR) {
            $xunit_path = Get-Command "xunit.console.clr4.exe" | Select-Object -ExpandProperty Definition
            $extra = "/appveyor"
        }
        else {
            $xunit_path = Resolve-Path $xunit
        }
        
        $opencover_path = Resolve-Path $opencover
        .$opencover_path `"-target:$xunit_path`" `"-targetargs:$base_dir\tests\$assembly\bin\$config\$assembly.dll /noshadow $extra`" `"-filter:$coverage_filter`" -mergeoutput `"-output:$coverage_file`" -register:user -returntargetcode
    }
}
