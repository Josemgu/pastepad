@echo off
REM Quita pastepad. Pregunta antes de tocar los datos del usuario:
REM snippets.json e historial.json son suyos, no del programa.

setlocal
set DESTINO=%LOCALAPPDATA%\pastepad
set MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs\pastepad.lnk

echo.
echo   pastepad - desinstalar
echo   ======================
echo.

echo   Cerrando el programa...
taskkill /F /IM pastepad.exe >nul 2>&1
timeout /t 2 /nobreak >nul

echo   Quitando el arranque con Windows
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v pastepad /f >nul 2>&1
REM La v1 y la v2 se registraban con otro nombre; se limpia tambien.
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v GestorSnippets /f >nul 2>&1

if exist "%MENU%" del /q "%MENU%"

echo.
echo   Tus textos guardados estan en:
echo     %DESTINO%\snippets.json
echo     %DESTINO%\historial.json
echo.
set /p BORRAR=  Borrar tambien tus datos? (s/N):
if /I "%BORRAR%"=="s" (
  rmdir /s /q "%DESTINO%" 2>nul
  echo   Todo borrado.
) else (
  echo   Guardando una copia en el escritorio antes de borrar el programa...
  if exist "%DESTINO%\snippets.json" copy /y "%DESTINO%\snippets.json" "%USERPROFILE%\Desktop\pastepad-snippets.json" >nul
  if exist "%DESTINO%\historial.json" copy /y "%DESTINO%\historial.json" "%USERPROFILE%\Desktop\pastepad-historial.json" >nul
  rmdir /s /q "%DESTINO%" 2>nul
  echo   Copia dejada en el escritorio.
)

echo.
echo   pastepad desinstalado.
echo.
pause
endlocal
