@echo off
setlocal
set log=%temp%\Startup.log

date /t >> %log%
time /t >> %log%

REM always return 0 to not block the service from starting up
exit /b 0
