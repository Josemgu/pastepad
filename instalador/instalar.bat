@echo off
REM Instala pastepad para el usuario actual. No pide administrador.
REM
REM Se instala en %LOCALAPPDATA% y no en Archivos de programa a
REM proposito: pastepad guarda sus datos junto al ejecutable, y en
REM Archivos de programa Windows bloquea la escritura sin avisar. El
REM programa arrancaria pero no podria guardar nada.

setlocal
set DESTINO=%LOCALAPPDATA%\pastepad
set ORIGEN=%~dp0dist\pastepad

echo.
echo   pastepad 3.0.0 - instalacion
echo   ===========================
echo.

if not exist "%ORIGEN%\pastepad.exe" (
  echo   ERROR: no encuentro dist\pastepad\pastepad.exe
  echo   Compila primero con:  flet pack main.py --onedir ...
  echo.
  pause
  exit /b 1
)

REM Si ya estaba corriendo hay que cerrarlo o no se puede sobrescribir.
tasklist /FI "IMAGENAME eq pastepad.exe" 2>nul | find /I "pastepad.exe" >nul
if not errorlevel 1 (
  echo   Cerrando la version que ya estaba abierta...
  taskkill /F /IM pastepad.exe >nul 2>&1
  timeout /t 2 /nobreak >nul
)

echo   Copiando a %DESTINO%
if not exist "%DESTINO%" mkdir "%DESTINO%"
xcopy "%ORIGEN%" "%DESTINO%" /E /I /Y /Q >nul
if errorlevel 1 (
  echo   ERROR al copiar.
  pause
  exit /b 1
)

echo   Registrando el arranque con Windows
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v pastepad ^
  /t REG_SZ /d "\"%DESTINO%\pastepad.exe\"" /f >nul

echo   Creando acceso directo en el menu inicio
set MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs
powershell -NoProfile -Command ^
  "$s=(New-Object -ComObject WScript.Shell).CreateShortcut('%MENU%\pastepad.lnk');" ^
  "$s.TargetPath='%DESTINO%\pastepad.exe';" ^
  "$s.WorkingDirectory='%DESTINO%';" ^
  "$s.Description='Gestor de portapapeles';$s.Save()" >nul 2>&1

echo.
echo   Listo. Abriendo pastepad...
echo.
echo   Atajo:        Ctrl + Shift + V
echo   Instalado en: %DESTINO%
echo   Para cerrarlo: taskkill /IM pastepad.exe /F
echo.
start "" "%DESTINO%\pastepad.exe"
timeout /t 3 /nobreak >nul
endlocal
