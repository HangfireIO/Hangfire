#!/bin/bash

set -e;
export Hangfire_SqlServer_ConnectionStringTemplate="Server=tcp:127.0.0.1,1433;Database={0};User Id=sa;Password=Password12!;TrustServerCertificate=True;PoolBlockingPeriod=NeverBlock";

if hash dotnet 2>/dev/null; 
then
  dotnet test -c Release -f netcoreapp3.1 tests/Hangfire.Core.Tests;
  dotnet test -c Release -f net6.0 tests/Hangfire.Core.Tests;
  if hash sqlcmd 2>/dev/null;
  then
    dotnet test -c Release -f netcoreapp3.1 tests/Hangfire.SqlServer.Tests;
    dotnet test -c Release -f net6.0 tests/Hangfire.SqlServer.Tests;
  fi
fi
