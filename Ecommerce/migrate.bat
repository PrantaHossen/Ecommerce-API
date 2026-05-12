@echo off
set PROJECT=ECommerceAPI.Infrastructure
set STARTUP=Ecommerce.API

if "%1"=="add" (
    dotnet ef migrations add %2 --project %PROJECT% --startup-project %STARTUP%
) else if "%1"=="update" (
    dotnet ef database update --project %PROJECT% --startup-project %STARTUP%
) else if "%1"=="remove" (
    dotnet ef migrations remove --project %PROJECT% --startup-project %STARTUP%
) else if "%1"=="list" (
    dotnet ef migrations list --project %PROJECT% --startup-project %STARTUP%
) else (
    echo Usage:
    echo   .\migrate.bat add MigrationName
    echo   .\migrate.bat update
    echo   .\migrate.bat remove
    echo   .\migrate.bat list
)
