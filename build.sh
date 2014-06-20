#!/usr/bin/env bash
export EnableNuGetPackageRestore="true"
xbuild HangFire.Mono.sln
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/HangFire.Core.Tests/bin/Debug/HangFire.Core.Tests.dll
mono --runtime=v4.0 tools/xunit/xunit.console.clr4.x86.exe tests/HangFire.Redis.Tests/bin/Debug/HangFire.Redis.Tests.dll