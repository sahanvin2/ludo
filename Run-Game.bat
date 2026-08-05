@echo off
setlocal
cd /d "%~dp0"

set "GUI_EXE=%~dp0LudoGame.Gui\bin\Debug\net8.0-windows\LudoGame.Gui.exe"
if exist "%GUI_EXE%" (
	start "" "%GUI_EXE%"
	exit /b 0
)

if exist "%~dp0dotnet\dotnet.exe" (
	dotnet "%~dp0LudoGame.Gui\LudoGame.Gui.csproj"
	exit /b 0
)

where dotnet >nul 2>nul
if not errorlevel 1 (
	dotnet "%~dp0LudoGame.Gui\LudoGame.Gui.csproj"
	exit /b 0
)

echo Unable to find a GUI executable or a .NET 8 dotnet command.
pause
exit /b 1
