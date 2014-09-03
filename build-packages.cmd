@echo Off
set target=%1
if "%target%" == "" (
   set target=CI
)
set config=%2
if "%config%" == "" (
   set config=Release
)
set useilmerge=%3
if "%useilmerge%" == "" (
   set useilmerge=True
)

%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild Build\Build.proj /t:"%target%" /p:Configuration="%config%" /p:UseILMerge="%useilmerge%" /m /fl /flp:LogFile=msbuild.log;Verbosity=Normal;Encoding=UTF-8 /nr:false