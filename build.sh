#!/usr/bin/env bash
export EnableNuGetPackageRestore="true"
xbuild Hangfire.Mono.sln /verbosity:diagnostic
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/Hangfire.Core.Tests/bin/Debug/v4.5/Hangfire.Core.Tests.dll
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/Hangfire.Redis.Tests/bin/Debug/v4.5/Hangfire.Redis.Tests.dll
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/Hangfire.SqlServer.RabbitMq.Tests/bin/Debug/v4.5/Hangfire.SqlServer.RabbitMq.Tests.dll