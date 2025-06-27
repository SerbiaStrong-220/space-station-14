@echo off
set PDIR=%~dp0
cd %PDIR%Bin\Content.Client
call Content.Client.exe
cd %PDIR%
set PDIR=
pause