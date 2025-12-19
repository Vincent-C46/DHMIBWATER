@echo off
:: DHBIMWATER Revit Addin 배포 스크립트

:: 1. 폴더 정의
set ADDIN_ROOT=%AppData%\Autodesk\Revit\Addins\2025
set CONTENT_PATH=%ADDIN_ROOT%\DHBIMWATER
set SOURCE_PATH=C:\Users\user\Projects\DHBIMWATER\src\DHBIMWATER.Revit

:: 2. 대상 폴더 생성
if not exist "%CONTENT_PATH%" mkdir "%CONTENT_PATH%"

:: 3. .addin 파일 복사
echo Copying .addin file...
copy /Y "%SOURCE_PATH%\DHBIMWATER.addin" "%ADDIN_ROOT%\"
if errorlevel 1 (
    echo Failed to copy .addin file
    pause
    exit /b 1
)

:: 4. DLL 파일 복사
echo Copying DLL files...
copy /Y "%SOURCE_PATH%\bin\Release\net8.0-windows\DHBIMWATER.*.dll" "%CONTENT_PATH%\"
if errorlevel 1 (
    echo Failed to copy DHBIMWATER DLL files
    pause
    exit /b 1
)

:: 5. 추가 의존성 DLL 복사 (존재하는 경우만)
if exist "%SOURCE_PATH%\bin\Release\net8.0-windows\Fluent*.dll" (
    echo Copying Fluent DLL files...
    copy /Y "%SOURCE_PATH%\bin\Release\net8.0-windows\Fluent*.dll" "%CONTENT_PATH%\"
)

if exist "%SOURCE_PATH%\bin\Release\net8.0-windows\Microsoft.Extensions*.dll" (
    echo Copying Microsoft.Extensions DLL files...
    copy /Y "%SOURCE_PATH%\bin\Release\net8.0-windows\Microsoft.Extensions*.dll" "%CONTENT_PATH%\"
)

if exist "%SOURCE_PATH%\bin\Release\net8.0-windows\Newtonsoft*.dll" (
    echo Copying Newtonsoft DLL files...
    copy /Y "%SOURCE_PATH%\bin\Release\net8.0-windows\Newtonsoft*.dll" "%CONTENT_PATH%\"
)

echo.
echo Deployment completed successfully!
echo Addin location: %ADDIN_ROOT%\DHBIMWATER.addin
echo DLL location: %CONTENT_PATH%
echo.
echo Please restart Revit to load the addin.
pause
