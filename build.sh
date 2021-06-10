#!/bin/bash

set -e;
export Hangfire_SqlServer_ConnectionStringTemplate="Server=tcp:127.0.0.1,1433;Database={0};User Id=sa;Password=Password12!";

if hash dotnet 2>/dev/null; 
then
  dotnet test -c Release -f netcoreapp3.1 tests/Hangfire.Core.Tests/Hangfire.Core.Tests.csproj;
  if hash sqlcmd 2>/dev/null;
  then
    dotnet test -c Release -f netcoreapp3.1 tests/Hangfire.SqlServer.Tests/Hangfire.SqlServer.Tests.csproj;
  fi
fi
