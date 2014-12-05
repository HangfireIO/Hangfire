#!/usr/bin/env bash
set -e
mono --runtime=v4.0 .nuget/NuGet.exe install .nuget/packages.config -OutputDirectory packages
mono --runtime=v4.0 .nuget/NuGet.exe restore Hangfire.Mono.sln
xbuild Hangfire.Mono.sln /verbosity:minimal
mono --runtime=v4.0 ./packages/xunit.runners.1.9.2/tools/xunit.console.clr4.x86.exe tests/Hangfire.Core.Tests/bin/Debug/Hangfire.Core.Tests.dll
mono --runtime=v4.0 ./packages/xunit.runners.1.9.2/tools/xunit.console.clr4.x86.exe tests/Hangfire.SqlServer.RabbitMq.Tests/bin/Debug/Hangfire.SqlServer.RabbitMq.Tests.dll
