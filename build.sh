#!/usr/bin/env bash
export EnableNuGetPackageRestore="true"
xbuild Hangfire.Mono.sln
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/Hangfire.Core.Tests/bin/Debug/Hangfire.Core.Tests.dll
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/Hangfire.Redis.Tests/bin/Debug/Hangfire.Redis.Tests.dll
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/Hangfire.SqlServer.RabbitMq.Tests/bin/Debug/Hangfire.SqlServer.RabbitMq.Tests.dll