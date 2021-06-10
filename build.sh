#!/bin/bash

set -e;
if hash docker 2>/dev/null;
then
  echo "$DOCKER_PASSWORD" | docker login -u "$DOCKER_USERNAME" --password-stdin;
  sudo docker run --name=mssql2017 -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=Password12!' -p 1433:1433 -d microsoft/mssql-server-linux:2017-latest;
  export Hangfire_SqlServer_ConnectionStringTemplate="Server=tcp:127.0.0.1,1433;Database={0};User Id=sa;Password=Password12!";
fi

if hash mono 2>/dev/null; 
then
  nuget install .nuget/packages.config -OutputDirectory packages;
  nuget restore Hangfire.sln;
  msbuild /p:Configuration=Release /verbosity:minimal Hangfire.sln;
  mono ./packages/xunit.runner.console.2.2.0/tools/xunit.console.exe ./tests/Hangfire.Core.Tests/bin/Release/net452/Hangfire.Core.Tests.dll;
  if hash docker 2>/dev/null;
  then
    mono ./packages/xunit.runner.console.2.2.0/tools/xunit.console.exe ./tests/Hangfire.SqlServer.Tests/bin/Release/net452/Hangfire.SqlServer.Tests.dll;
  fi
fi

if hash dotnet 2>/dev/null; 
then
  dotnet test -c Release -f netcoreapp3.1 tests/Hangfire.Core.Tests/Hangfire.Core.Tests.csproj;
  if hash docker 2>/dev/null;
  then
    dotnet test -c Release -f netcoreapp3.1 tests/Hangfire.SqlServer.Tests/Hangfire.SqlServer.Tests.csproj;
  fi
fi
