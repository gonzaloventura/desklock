@echo off
echo Building DeskLock for Windows...
echo.

dotnet build -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo.
    echo To run:
    echo   dotnet run -c Release
    echo.
    echo To publish as single executable:
    echo   dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
    echo.
) else (
    echo.
    echo Build FAILED.
)
