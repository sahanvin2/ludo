@echo off
cd /d "%~dp0"
set DOTNET_ROOT=
.\dotnet\dotnet.exe run --project .\LudoGame.Gui\LudoGame.Gui.csproj
pause
