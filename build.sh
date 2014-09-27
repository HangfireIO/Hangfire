#!/usr/bin/env bash
export EnableNuGetPackageRestore="true"
xbuild Hangfire.Mono.net40.sln
xbuild Hangfire.Mono.net45.sln
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/Hangfire.Core.Tests/bin/Debug/net40/Hangfire.Core.Tests.dll
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/Hangfire.Core.Tests/bin/Debug/net45/Hangfire.Core.Tests.dll
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/Hangfire.Redis.Tests/bin/Debug/net40/Hangfire.Redis.Tests.dll
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/Hangfire.Redis.Tests/bin/Debug/net45/Hangfire.Redis.Tests.dll
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/Hangfire.SqlServer.RabbitMq.Tests/bin/Debug/net40/Hangfire.SqlServer.RabbitMq.Tests.dll
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/Hangfire.SqlServer.RabbitMq.Tests/bin/Debug/net45/Hangfire.SqlServer.RabbitMq.Tests.dll