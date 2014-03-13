@echo Off

set config=%1
if "%config%" == "" (
   set config=Release
)
set version=%2
if "%version%" == "" (
   set version=1
)
set target=%3
if "%target%"=="" (
   set target=CI
)
set perfRun=%4
if /i not "%perfRun%" == "true" (
   set perfRun=false
)
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild Build\Build.proj /t:%target% /p:Configuration="%config%";BUILD_NUMBER=%version%;PRERELEASE=true /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false /p:PerfRun=%perfRun%