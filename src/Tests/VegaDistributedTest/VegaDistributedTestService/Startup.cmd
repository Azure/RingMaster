@echo off
setlocal
set log=C:\Resources\Directory\Startup.log

date /t >> %log%
time /t >> %log%
netsh int ipv4 show dynamicport tcp >> %log%

for /f "usebackq skip=4 tokens=1,5 delims= " %%i in (`netsh int ipv4 show dynamicport tcp`) do (
    if %%i equ Number (
        if %%j leq 50000 (
            echo "Fix dynamic port range" >> %log%
            netsh int ipv4 set dynamicport tcp start=1025 num=64510
        ) else (
            echo "Dynamic port range is unchanged" >> %log%
        )
    )
)