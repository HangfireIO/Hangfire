Properties {
    $solution = "Hangfire.sln"
}

Include ".\psake-common.ps1"

Task Default -Depends Test

Task Test -depends Compile {
    Run-Tests "Hangfire.Core.Tests"
    Run-Tests "Hangfire.SqlServer.Tests"
    Run-Tests "Hangfire.SqlServer.Msmq.Tests"
}

Task Merge -depends Test {
    Merge-Assembly "Hangfire.Core" @("NCrontab", "CronExpressionDescriptor", "Microsoft.Owin")
    Merge-Assembly "Hangfire.SqlServer" @("Dapper")
}

Task Collect -depends Merge {
    Collect-Content "content\readme.txt"

    Collect-Assembly "Hangfire.Core" "Net45"

    Collect-Assembly "Hangfire.SqlServer" "Net45"
    Collect-Tool "src\Hangfire.SqlServer\Install.sql"

    Collect-Assembly "Hangfire.SqlServer.Msmq" "Net45"
    Collect-Assembly "Hangfire.SqlServer.RabbitMq" "Net45"
}

Task Pack -depends Collect {
    Create-Package "Hangfire"
    Create-Package "Hangfire.Core"
    Create-Package "Hangfire.SqlServer"
    Create-Package "Hangfire.SqlServer.Msmq"
    Create-Package "Hangfire.SqlServer.RabbitMq"
}
