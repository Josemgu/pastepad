@echo off
REM Genera pastepad.exe (un solo archivo, sin consola).
REM Doble clic para ejecutar.

echo Instalando dependencias...
pip install -r requirements.txt
pip install pyinstaller

echo.
echo Ejecutando las pruebas...
python prueba.py
if errorlevel 1 (
  echo.
  echo Hay pruebas que fallan. Revisa antes de compilar.
  pause
  exit /b 1
)

echo.
echo Generando el ejecutable...
pyinstaller --onefile --noconsole --name pastepad ^
  --icon docs\pastepad.ico ^
  --add-data "docs\pastepad.ico;." ^
  --version-file version.txt ^
  --collect-all customtkinter ^
  main.pyw

echo.
echo Listo. El programa esta en la carpeta "dist".
pause
