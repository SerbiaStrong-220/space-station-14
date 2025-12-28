@echo off
set PDIR=%~dp0
cd %PDIR%

echo [DEBUG] Check RobustToolbox... Use: RUN_THIS.py
python RUN_THIS.py

echo.
echo [DEBUG] Dotnet build... Use: dotnet build -c Release
dotnet build -c Release

cd %PDIR%
set PDIR=
pause