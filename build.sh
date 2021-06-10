#!/bin/bash

set -e;

if hash mono 2>/dev/null; 
then
  nuget install .nuget/packages.config -OutputDirectory packages;
  nuget restore Hangfire.sln;
  msbuild /p:Configuration=Release /verbosity:minimal Hangfire.sln;
  mono ./packages/xunit.runner.console.2.2.0/tools/xunit.console.exe ./tests/Hangfire.Core.Tests/bin/Release/net452/Hangfire.Core.Tests.dll;
  if hash sqlcmd 2>/dev/null;
  then
    mono ./packages/xunit.runner.console.2.2.0/tools/xunit.console.exe ./tests/Hangfire.SqlServer.Tests/bin/Release/net452/Hangfire.SqlServer.Tests.dll;
  fi
fi

if hash dotnet 2>/dev/null; 
then
  dotnet test -c Release -f netcoreapp3.1 tests/Hangfire.Core.Tests/Hangfire.Core.Tests.csproj;
  if hash sqlcmd 2>/dev/null;
  then
    dotnet test -c Release -f netcoreapp3.1 tests/Hangfire.SqlServer.Tests/Hangfire.SqlServer.Tests.csproj;
  fi
fi
